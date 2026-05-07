import { useFormik } from "formik";
import { AxiosError } from "axios";
import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import { LogOut } from "lucide-react";
import {
  generateStatementPdf,
  lookupAccount,
  previewStatement,
  type StatementRequest,
} from "../services/statement";
import { logoutUser } from "../services/session";
import PreviewChargesModal from "./PreviewChargesModal";

type StatementPreviewResponse = {
  previewToken?: string;
  numberOfPages: number;
  totalCharge: number;
  accountToCharge: string;
  chargeMessage: string;
  accountName: string;
  accountNumber: string;
};

const VisaStatement = () => {
  const formatGhsAmount = (amount: number) => `GHS ${amount.toFixed(2)}`;

  const navigate = useNavigate();
  const [isLookupLoading, setIsLookupLoading] = useState(false);
  const [accountName, setAccountName] = useState("");
  const [lookupError, setLookupError] = useState("");
  const [previewResults, setPreviewResults] =
    useState<StatementPreviewResponse | null>(null);
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);
  const [isPrintLoading, setIsPrintLoading] = useState(false);
  const [isPreviewModalOpen, setIsPreviewModalOpen] = useState(false);
  const [previewError, setPreviewError] = useState("");
  const chargeAccountInputRef = useRef<HTMLInputElement>(null);
  const latestLookupRequestIdRef = useRef(0);
  const lastResolvedAccountNumberRef = useRef("");

  const buildRequestPayload = (values: {
    accountNumber: string;
    startDate: string;
    endDate: string;
    chargeAccNumber: string;
    chargeAltAccount: boolean;
    waiveCharge: boolean;
  }): StatementRequest => {
    const authUserRaw = localStorage.getItem("authUser");
    const authUser = authUserRaw ? JSON.parse(authUserRaw) : null;

    return {
      accountNumber: values.accountNumber,
      startDate: values.startDate,
      endDate: values.endDate,
      channel: "VISA",
      waiveCharge: values.waiveCharge,
      chargeAltAccount: values.chargeAltAccount,
      altAccountNumber: values.chargeAltAccount
        ? values.chargeAccNumber
        : undefined,
      staffUsername: authUser?.username ?? "SYSTEM",
    };
  };

  const handlePrint = async () => {
    setPreviewError("");
    setIsPrintLoading(true);

    try {
      const payload = {
        ...buildRequestPayload(formik.values),
        previewToken: previewResults?.previewToken,
      };
      const pdfBlob = await generateStatementPdf(payload);

      const downloadUrl = window.URL.createObjectURL(pdfBlob);
      const link = document.createElement("a");
      link.href = downloadUrl;
      link.download = `VISA_Statement_${payload.accountNumber}.pdf`;
      link.click();
      window.URL.revokeObjectURL(downloadUrl);

      setIsPreviewModalOpen(false);
    } catch (error) {
      if (error instanceof AxiosError && error.code === "ERR_CANCELED") {
        return;
      }

      if (error instanceof AxiosError) {
        setPreviewError(
          error.response?.data?.message ?? "Print failed. Please try again.",
        );
      } else {
        setPreviewError("Print failed. Please try again.");
      }
    } finally {
      setIsPrintLoading(false);
    }
  };

  const formik = useFormik({
    initialValues: {
      accountNumber: "",
      startDate: "",
      endDate: "",
      chargeAccNumber: "",
      chargeAltAccount: false,
      waiveCharge: false,
    },
    onSubmit: async (values) => {
      setIsPreviewLoading(true);
      setPreviewError("");
      setPreviewResults(null);
      setIsPreviewModalOpen(false);

      try {
        const payload = buildRequestPayload(values);

        const response = await previewStatement(payload);

        // If waive charge is checked, set total charge to 0
        const result = {
          ...response,
          totalCharge: values.waiveCharge ? 0 : response.totalCharge,
        };

        setPreviewResults(result);
        setIsPreviewModalOpen(true);
      } catch (error) {
        if (error instanceof AxiosError && error.code === "ERR_CANCELED") {
          return;
        }

        if (error instanceof AxiosError) {
          setPreviewError(
            error.response?.data?.message ??
              "Unable to preview statement. Please try again.",
          );
        } else {
          setPreviewError("Unable to preview statement.");
        }
      } finally {
        setIsPreviewLoading(false);
      }
    },
  });

  const chargeAltAccount = formik.values.chargeAltAccount;
  const setFieldValue = formik.setFieldValue;

  const handlePrimaryAccountNumberChange = (
    e: React.ChangeEvent<HTMLInputElement>,
  ) => {
    const normalizedAccountNumber = e.target.value
      .replace(/\D/g, "")
      .slice(0, 13);
    formik.setFieldValue("accountNumber", normalizedAccountNumber);

    if (
      !formik.values.chargeAltAccount &&
      normalizedAccountNumber.length < 13
    ) {
      formik.setFieldValue("chargeAccNumber", "");
    }
  };

  const handleChargeAccountNumberChange = (
    e: React.ChangeEvent<HTMLInputElement>,
  ) => {
    const normalizedAccountNumber = e.target.value
      .replace(/\D/g, "")
      .slice(0, 13);
    formik.setFieldValue("chargeAccNumber", normalizedAccountNumber);
  };

  const handleChargeAltAccountChange = (
    e: React.ChangeEvent<HTMLInputElement>,
  ) => {
    const isAltAccountChecked = e.target.checked;
    formik.setFieldValue("chargeAltAccount", isAltAccountChecked);

    if (isAltAccountChecked) {
      formik.setFieldValue("chargeAccNumber", "");
      window.requestAnimationFrame(() => {
        chargeAccountInputRef.current?.focus();
      });
      return;
    }

    if (formik.values.accountNumber.length === 13 && accountName) {
      formik.setFieldValue("chargeAccNumber", formik.values.accountNumber);
    }
  };

  const lookupAccountName = useCallback(
    async (accountNumber: string) => {
      const normalizedAccountNumber = accountNumber.trim();

      if (normalizedAccountNumber.length !== 13) {
        return;
      }

      if (normalizedAccountNumber === lastResolvedAccountNumberRef.current) {
        return;
      }

      const requestId = latestLookupRequestIdRef.current + 1;
      latestLookupRequestIdRef.current = requestId;

      setIsLookupLoading(true);
      setLookupError("");

      try {
        const account = await lookupAccount(normalizedAccountNumber, "VISA");

        if (requestId !== latestLookupRequestIdRef.current) {
          return;
        }

        lastResolvedAccountNumberRef.current = normalizedAccountNumber;
        setAccountName(account.accountName);

        if (!chargeAltAccount) {
          setFieldValue("chargeAccNumber", normalizedAccountNumber);
        }
      } catch (error) {
        if (requestId !== latestLookupRequestIdRef.current) {
          return;
        }

        if (error instanceof AxiosError && error.code === "ERR_CANCELED") {
          return;
        }

        lastResolvedAccountNumberRef.current = "";
        setAccountName("");
        if (!chargeAltAccount) {
          setFieldValue("chargeAccNumber", "");
        }
        if (error instanceof AxiosError) {
          setLookupError(
            error.response?.data?.message ??
              "Unable to resolve account name. Please verify account number.",
          );
        } else {
          setLookupError("Unable to resolve account name.");
        }
      } finally {
        if (requestId === latestLookupRequestIdRef.current) {
          setIsLookupLoading(false);
        }
      }
    },
    [chargeAltAccount, setFieldValue],
  );

  useEffect(() => {
    const accountNumber = formik.values.accountNumber.trim();

    if (!accountNumber) {
      setAccountName("");
      setLookupError("");
      lastResolvedAccountNumberRef.current = "";
      if (!chargeAltAccount) {
        setFieldValue("chargeAccNumber", "");
      }
      return;
    }

    if (accountNumber.length < 13) {
      setAccountName("");
      setLookupError("");
      lastResolvedAccountNumberRef.current = "";
      if (!chargeAltAccount) {
        setFieldValue("chargeAccNumber", "");
      }
      return;
    }

    const timeoutId = window.setTimeout(() => {
      void lookupAccountName(accountNumber);
    }, 250);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [
    formik.values.accountNumber,
    chargeAltAccount,
    lookupAccountName,
    setFieldValue,
  ]);

  const blockManualDateTyping = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key.length === 1 || e.key === "Backspace" || e.key === "Delete") {
      e.preventDefault();
    }
  };

  return (
    <main className="min-h-screen bg-[#f7f8fc]">
      <div className="flex justify-end px-6 pt-6">
        <button
          type="button"
          onClick={() => logoutUser(navigate)}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-100"
        >
          <LogOut size={16} />
          Logout
        </button>
      </div>

      <section className="flex justify-center px-4 pt-6">
        <div className="w-full max-w-3xl rounded-2xl border border-slate-200 bg-white/90 px-6 py-8 text-center shadow-sm backdrop-blur-sm sm:px-10 sm:py-10">
          <h1 className="text-3xl font-semibold tracking-[0.18em] text-slate-900 sm:text-4xl">
            VISA STATEMENT
          </h1>
          <h2 className="mt-4 text-lg font-medium text-slate-600 sm:text-xl">
            Welcome to your VISA Statement
          </h2>
          <p className="mt-2 text-sm leading-6 text-slate-500 sm:text-base">
            Enter the details to preview charges
          </p>
        </div>
      </section>

      <div className="flex items-center justify-center px-4 py-8">
        <div className="w-full max-w-4xl rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
          <form
            onSubmit={formik.handleSubmit}
            className="grid w-full grid-cols-[200px_1fr] items-center gap-x-4 gap-y-10"
          >
            <label
              htmlFor="accountNumber"
              className="text-sm font-medium text-slate-700"
            >
              Account Number:
            </label>
            <input
              id="accountNumber"
              name="accountNumber"
              value={formik.values.accountNumber}
              onChange={handlePrimaryAccountNumberChange}
              onBlur={formik.handleBlur}
              inputMode="numeric"
              maxLength={13}
              minLength={13}
              pattern="[0-9]{13}"
              required
              title="Fill out this field with a valid account number"
              placeholder="Enter Account Number"
              className="w-full rounded border p-2"
            />

            {lookupError && (
              <div className="col-span-2">
                <p className="rounded-md bg-red-50 p-3 text-sm text-red-700 border border-red-200">
                  {lookupError}
                </p>
              </div>
            )}

            <label className="text-sm font-medium text-slate-700">
              Account Name:
            </label>
            <input
              value={isLookupLoading ? "Loading account name..." : accountName}
              readOnly
              placeholder="Account name will appear here"
              className="w-full rounded border bg-slate-50 p-2 text-slate-700"
            />

            <label
              htmlFor="startDate"
              className="text-sm font-medium text-slate-700"
            >
              Start Date:
            </label>
            <div className="relative">
              <input
                id="startDate"
                type="date"
                name="startDate"
                value={formik.values.startDate}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                onKeyDown={blockManualDateTyping}
                onPaste={(e) => e.preventDefault()}
                className="w-full rounded border p-2 pr-10"
              />
            </div>

            <label
              htmlFor="endDate"
              className="text-sm font-medium text-slate-700"
            >
              End Date:
            </label>
            <div className="relative">
              <input
                id="endDate"
                type="date"
                name="endDate"
                value={formik.values.endDate}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                onKeyDown={blockManualDateTyping}
                onPaste={(e) => e.preventDefault()}
                className="w-full rounded border p-2 pr-10"
              />
            </div>

            <label
              htmlFor="chargeAltAccount"
              className="text-sm font-medium text-slate-700"
            >
              Charge Alt Account:
            </label>
            <input
              id="chargeAltAccount"
              type="checkbox"
              name="chargeAltAccount"
              checked={formik.values.chargeAltAccount}
              onChange={handleChargeAltAccountChange}
              className="h-4 w-4 justify-self-start accent-amber-500"
            />

            <label
              htmlFor="chargeAccNumber"
              className="text-sm font-medium text-slate-700"
            >
              Charge Account Number:
            </label>
            <input
              ref={chargeAccountInputRef}
              id="chargeAccNumber"
              name="chargeAccNumber"
              value={formik.values.chargeAccNumber}
              onChange={handleChargeAccountNumberChange}
              inputMode="numeric"
              maxLength={13}
              minLength={13}
              pattern="[0-9]{13}"
              title="Fill out this field with a valid account number"
              placeholder=" "
              className="w-full rounded border p-2"
            />
            <label
              htmlFor="waiveCharge"
              className="text-sm font-medium text-slate-700"
            >
              Waive Charge:
            </label>
            <input
              id="waiveCharge"
              type="checkbox"
              name="waiveCharge"
              checked={formik.values.waiveCharge}
              onChange={formik.handleChange}
              className="h-4 w-4 justify-self-start accent-amber-500"
            />

            <div className="col-span-2 flex justify-center pt-2">
              <button
                type="submit"
                disabled={isPreviewLoading}
                className="rounded bg-amber-400 px-4 py-2 text-white disabled:cursor-not-allowed disabled:opacity-50"
              >
                {isPreviewLoading ? "Loading Preview..." : "Preview Charges"}
              </button>
            </div>
          </form>
        </div>
      </div>

      {previewError && (
        <p className="mb-4 text-center text-sm text-red-600">{previewError}</p>
      )}

      <PreviewChargesModal
        isOpen={isPreviewModalOpen}
        title="Preview Charges"
        message={previewResults?.chargeMessage ?? "Preview generated."}
        accountNumber={previewResults?.accountNumber ?? ""}
        accountName={previewResults?.accountName ?? ""}
        numberOfPages={previewResults?.numberOfPages ?? 0}
        totalChargeText={
          formik.values.waiveCharge
            ? `${formatGhsAmount(0)} (Waived)`
            : formatGhsAmount(previewResults?.totalCharge ?? 0)
        }
        accountToCharge={previewResults?.accountToCharge}
        isPrinting={isPrintLoading}
        onPrint={() => {
          void handlePrint();
        }}
        onCancel={() => setIsPreviewModalOpen(false)}
      />
    </main>
  );
};

export default VisaStatement;
