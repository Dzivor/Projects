import { useFormik } from "formik";
import { AxiosError } from "axios";
import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import { LogOut } from "lucide-react";
import {
  generateStatementPdf,
  getBackendErrorMessage,
  lookupAccount,
  previewStatement,
  type StatementPreviewResponse,
  type StatementRequest,
} from "../services/statement";
import { logoutUser } from "../services/session";
import BackButton from "./BackButton";
import PreviewChargesModal from "./PreviewChargesModal";

const getWelcomeName = (): string => {
  const authUserRaw = localStorage.getItem("authUser");

  if (!authUserRaw) {
    return "Guest";
  }

  try {
    const authUser = JSON.parse(authUserRaw) as {
      firstName?: string;
      fullName?: string;
    };

    const firstName = authUser.firstName?.trim();

    if (firstName) {
      return firstName;
    }

    const fullName = authUser.fullName?.trim();

    if (!fullName) {
      return "Guest";
    }

    return fullName.split(/\s+/)[0] || "Guest";
  } catch {
    return "Guest";
  }
};

const ESBStatement = () => {
  const userName = getWelcomeName();

  const navigate = useNavigate();
  const [isPrintLoading, setIsPrintLoading] = useState(false);
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);
  const [isPreviewModalOpen, setIsPreviewModalOpen] = useState(false);
  const [isLookupLoading, setIsLookupLoading] = useState(false);
  const [accountName, setAccountName] = useState("");
  const [previewResults, setPreviewResults] =
    useState<StatementPreviewResponse | null>(null);
  const [previewErrorMessage, setPreviewErrorMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const latestLookupRequestIdRef = useRef(0);
  const lastResolvedAccountNumberRef = useRef("");
  const lastPreviewValuesRef = useRef<{
    accountNumber: string;
    startDate: string;
    endDate: string;
  } | null>(null);

  const buildRequestPayload = (values: {
    accountNumber: string;
    startDate: string;
    endDate: string;
  }): StatementRequest => {
    const authUserRaw = localStorage.getItem("authUser");
    const authUser = authUserRaw ? JSON.parse(authUserRaw) : null;

    return {
      accountNumber: values.accountNumber,
      startDate: values.startDate,
      endDate: values.endDate,
      channel: "ESB",
      waiveCharge: false,
      chargeAltAccount: false,
      staffUsername: authUser?.username ?? "dev.user",
    };
  };

  const runPreview = async (values: {
    accountNumber: string;
    startDate: string;
    endDate: string;
  }) => {
    lastPreviewValuesRef.current = values;
    setPreviewErrorMessage("");
    setPreviewResults(null);
    setIsPreviewModalOpen(true);
    setIsPreviewLoading(true);

    try {
      const payload = buildRequestPayload(values);
      const response = await previewStatement(payload);
      setPreviewResults(response);
    } catch (error) {
      if (error instanceof AxiosError && error.code === "ERR_CANCELED") {
        return;
      }

      setPreviewErrorMessage("Unable to load preview. Please try again.");
    } finally {
      setIsPreviewLoading(false);
    }
  };

  const handlePrint = async () => {
    setErrorMessage("");
    setIsPrintLoading(true);

    try {
      const values = formik.values;
      const payload = {
        ...buildRequestPayload(values),
        previewToken: previewResults?.previewToken,
      };
      const pdfBlob = await generateStatementPdf(payload);

      const downloadUrl = window.URL.createObjectURL(pdfBlob);
      const link = document.createElement("a");
      link.href = downloadUrl;
      link.download = `ESB_Statement_${payload.accountNumber}.pdf`;
      link.click();
      window.URL.revokeObjectURL(downloadUrl);

      setIsPreviewModalOpen(false);
    } catch (error) {
      if (error instanceof AxiosError && error.code === "ERR_CANCELED") {
        return;
      }

      setIsPreviewModalOpen(false);
      setErrorMessage(
        await getBackendErrorMessage(error, "Print failed. Please try again."),
      );
    } finally {
      setIsPrintLoading(false);
    }
  };

  const formik = useFormik({
    initialValues: {
      accountNumber: "",
      startDate: "",
      endDate: "",
    },
    onSubmit: async (values) => {
      void runPreview(values);
    },
  });

  const handlePrimaryAccountNumberChange = (
    e: React.ChangeEvent<HTMLInputElement>,
  ) => {
    const normalizedAccountNumber = e.target.value
      .replace(/\D/g, "")
      .slice(0, 13);
    formik.setFieldValue("accountNumber", normalizedAccountNumber);
  };

  const lookupAccountName = useCallback(async (accountNumber: string) => {
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
    setErrorMessage("");

    try {
      const account = await lookupAccount(normalizedAccountNumber, "ESB");

      if (requestId !== latestLookupRequestIdRef.current) {
        return;
      }

      lastResolvedAccountNumberRef.current = normalizedAccountNumber;
      setAccountName(account.accountName);
    } catch (error) {
      if (requestId !== latestLookupRequestIdRef.current) {
        return;
      }

      if (error instanceof AxiosError && error.code === "ERR_CANCELED") {
        return;
      }

      lastResolvedAccountNumberRef.current = "";
      setAccountName("");
      if (error instanceof AxiosError) {
        setErrorMessage(
          error.response?.data?.message ??
            "Unable to resolve account name. Please try again later.",
        );
      } else {
        setErrorMessage("Unable to resolve account name.");
      }
    } finally {
      if (requestId === latestLookupRequestIdRef.current) {
        setIsLookupLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    const accountNumber = formik.values.accountNumber.trim();

    if (!accountNumber) {
      setAccountName("");
      setErrorMessage("");
      lastResolvedAccountNumberRef.current = "";
      return;
    }

    if (accountNumber.length < 13) {
      setAccountName("");
      setErrorMessage("");
      lastResolvedAccountNumberRef.current = "";
      return;
    }

    const timeoutId = window.setTimeout(() => {
      void lookupAccountName(accountNumber);
    }, 250);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [formik.values.accountNumber, lookupAccountName]);

  const blockManualDateTyping = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key.length === 1 || e.key === "Backspace" || e.key === "Delete") {
      e.preventDefault();
    }
  };

  return (
    <main className="relative min-h-screen bg-[#f7f8fc]">
      <div className="absolute left-6 top-6 z-10">
        <BackButton />
      </div>

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

      <div className="text-center py-8 px-4">
        <h1 className="text-3xl font-semibold tracking-[0.18em] text-slate-900 sm:text-4xl">
          ESB STATEMENT
        </h1>
        <h2 className="mt-3 text-lg font-medium text-slate-600 sm:text-xl">
          Welcome, {userName}.
        </h2>
        <p className="mt-1 text-sm leading-6 text-slate-500 sm:text-base">
          Enter the details to print your statement
        </p>
      </div>

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
              title="Account number must be exactly 13 digits"
              placeholder="Enter Account Number"
              className="w-full rounded border p-2"
            />

            {errorMessage && (
              <div className="col-span-2">
                <p className="rounded-md bg-red-50 p-3 text-sm text-red-700 border border-red-200">
                  {errorMessage}
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
                required
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
                required
                onKeyDown={blockManualDateTyping}
                onPaste={(e) => e.preventDefault()}
                className="w-full rounded border p-2 pr-10"
              />
            </div>

            <div className="col-span-2 flex justify-center">
              <button
                type="submit"
                disabled={isPreviewLoading}
                className="rounded bg-amber-400 px-4 py-2 text-white"
              >
                {isPreviewLoading ? "Loading Preview..." : "Print"}
              </button>
            </div>
          </form>
        </div>
      </div>

      <PreviewChargesModal
        isOpen={isPreviewModalOpen}
        title="Preview Charges"
        accountNumber={
          previewResults?.accountNumber ?? formik.values.accountNumber
        }
        accountName={previewResults?.accountName ?? accountName}
        previewData={previewResults}
        channel="ESB"
        waiveCharge={false}
        chargeAltAccount={false}
        isPreviewLoading={isPreviewLoading}
        previewErrorMessage={previewErrorMessage}
        isPrinting={isPrintLoading}
        onPrint={() => {
          void handlePrint();
        }}
        onCancel={() => setIsPreviewModalOpen(false)}
        onRetry={() => {
          if (lastPreviewValuesRef.current) {
            void runPreview(lastPreviewValuesRef.current);
          }
        }}
      />
    </main>
  );
};

export default ESBStatement;
