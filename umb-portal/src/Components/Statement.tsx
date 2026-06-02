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
import ErrorModal from "./ErrorModal";

type VisaPreviewValues = {
  accountNumber: string;
  startDate: string;
  endDate: string;
  chargeAccNumber: string;
  chargeAltAccount: boolean;
  waiveCharge: boolean;
};

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

const VisaStatement = () => {
  const userName = getWelcomeName();

  const navigate = useNavigate();
  const [isLookupLoading, setIsLookupLoading] = useState(false);
  const [accountName, setAccountName] = useState("");
  const [accountBalance, setAccountBalance] = useState<number | null>(null);
  const [lookupError, setLookupError] = useState("");
  const [previewResults, setPreviewResults] =
    useState<StatementPreviewResponse | null>(null);
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);
  const [isPrintLoading, setIsPrintLoading] = useState(false);
  const [isPreviewModalOpen, setIsPreviewModalOpen] = useState(false);
  const [previewErrorMessage, setPreviewErrorMessage] = useState("");
  const [isErrorModalOpen, setIsErrorModalOpen] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const chargeAccountInputRef = useRef<HTMLInputElement>(null);
  const latestLookupRequestIdRef = useRef(0);
  const lastResolvedAccountNumberRef = useRef("");
  const lastPreviewValuesRef = useRef<VisaPreviewValues | null>(null);

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

  const runPreview = async (values: VisaPreviewValues) => {
    lastPreviewValuesRef.current = values;
    setPreviewErrorMessage("");
    setPreviewResults(null);
    setIsPreviewModalOpen(true);
    setIsPreviewLoading(true);

    try {
      const payload = buildRequestPayload(values);
      const response = await previewStatement(payload);
      setPreviewResults({
        ...response,
        accountBalance: accountBalance ?? response.bookBalance,
      });
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

      setIsPreviewModalOpen(false);
      const message = await getBackendErrorMessage(
        error,
        "Print failed. Please try again.",
      );
      setErrorMessage(message);
      setIsErrorModalOpen(true);
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
      void runPreview(values);
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
        setAccountBalance(account.accountBalance);

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
        setAccountBalance(null);
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
      setAccountBalance(null);
      setLookupError("");
      lastResolvedAccountNumberRef.current = "";
      if (!chargeAltAccount) {
        setFieldValue("chargeAccNumber", "");
      }
      return;
    }

    if (accountNumber.length < 13) {
      setAccountName("");
      setAccountBalance(null);
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
    <main className="relative min-h-screen text-slate-50 bg-astek-pattern ">
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
      <div className="flex items-center justify-center px-4 py-8">
        <div className="w-full max-w-3xl rounded-2xl border border-white/30 bg-astek-pattern-panel p-6 text-slate-900 shadow-md backdrop-blur-sm sm:p-8">
          <div className="text-center py-8 px-4">
            <h2 className="text-3xl font-bold tracking-[0.18em] text-slate-950 drop-shadow-sm sm:text-4xl">
              VISA STATEMENT.
            </h2>
            <h2 className="mt-3 text-lg font-semibold text-slate-800 sm:text-xl">
              Welcome, {userName}.
            </h2>
            <p className="mt-1 text-sm leading-6 text-slate-700 sm:text-base">
              Enter the details to preview charges
            </p>
          </div>

          <div className="bg-white border-0 rounded-lg p-6 shadow-[10px_10px_20px_0px_rgba(0,0,0,0.1)]">
            <form
              onSubmit={formik.handleSubmit}
              className="grid w-full grid-cols-[200px_1fr] items-center gap-x-4 gap-y-10"
            >
              <label
                htmlFor="accountNumber"
                className="text-sm font-semibold text-slate-900"
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
                  <p className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                    {lookupError}
                  </p>
                </div>
              )}

              <label className="text-sm font-semibold text-slate-900">
                Account Name:
              </label>
              <input
                value={
                  isLookupLoading ? "Loading account name..." : accountName
                }
                readOnly
                placeholder="Account name will appear here"
                className="w-full rounded border bg-slate-50 p-2 text-slate-500"
              />

              <label
                htmlFor="startDate"
                className="text-sm font-semibold text-slate-900"
              >
                Start Date:
              </label>
              <div className="relative">
                <input
                  id="startDate"
                  type="date"
                  name="startDate"
                  required
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
                className="text-sm font-semibold text-slate-900"
              >
                End Date:
              </label>
              <div className="relative">
                <input
                  id="endDate"
                  type="date"
                  name="endDate"
                  required
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
                className="text-sm font-semibold text-slate-900"
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
                className="text-sm font-medium text-slate-900"
              >
                Charge Account Number:
              </label>
              <input
                ref={chargeAccountInputRef}
                id="chargeAccNumber"
                name="chargeAccNumber"
                value={formik.values.chargeAccNumber}
                onChange={handleChargeAccountNumberChange}
                disabled={!formik.values.chargeAltAccount}
                inputMode="numeric"
                maxLength={13}
                minLength={13}
                pattern="[0-9]{13}"
                title="Fill out this field with a valid UMB account number"
                placeholder=" "
                className="w-full rounded border p-2"
              />
              <label
                htmlFor="waiveCharge"
                className="text-sm font-medium text-slate-900"
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
      </div>

      <PreviewChargesModal
        isOpen={isPreviewModalOpen}
        title="Preview Charges"
        accountNumber={previewResults?.accountNumber ?? ""}
        accountName={previewResults?.accountName ?? ""}
        previewData={previewResults}
        channel="VISA"
        waiveCharge={formik.values.waiveCharge}
        chargeAltAccount={formik.values.chargeAltAccount}
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

      <ErrorModal
        isOpen={isErrorModalOpen}
        title="Error"
        message={errorMessage}
        onClose={() => {
          setIsErrorModalOpen(false);
          setErrorMessage("");
        }}
      />
    </main>
  );
};

export default VisaStatement;
