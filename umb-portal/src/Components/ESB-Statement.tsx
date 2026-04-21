import { useFormik } from "formik";
import { AxiosError } from "axios";
import { useEffect, useState } from "react";
import { CalendarDays, LogOut } from "lucide-react";
import {
  generateStatementPdf,
  lookupAccount,
  type StatementRequest,
} from "../services/statement";

const ESBStatement = () => {
  const [isPrintLoading, setIsPrintLoading] = useState(false);
  const [isLookupLoading, setIsLookupLoading] = useState(false);
  const [accountName, setAccountName] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

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

  const handlePrint = async (values: {
    accountNumber: string;
    startDate: string;
    endDate: string;
  }) => {
    setErrorMessage("");
    setIsPrintLoading(true);

    try {
      const payload = buildRequestPayload(values);
      const pdfBlob = await generateStatementPdf(payload);

      const downloadUrl = window.URL.createObjectURL(pdfBlob);
      const link = document.createElement("a");
      link.href = downloadUrl;
      link.download = `ESB_Statement_${payload.accountNumber}.pdf`;
      link.click();
      window.URL.revokeObjectURL(downloadUrl);
    } catch (error) {
      if (error instanceof AxiosError) {
        setErrorMessage(
          error.response?.data?.message ?? "Print failed. Please try again.",
        );
      } else {
        setErrorMessage("Print failed. Please try again.");
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
    },
    onSubmit: async (values) => {
      await handlePrint(values);
    },
  });

  const lookupAccountName = async (accountNumber: string) => {
    const normalizedAccountNumber = accountNumber.trim();

    if (!normalizedAccountNumber) {
      setAccountName("");
      setErrorMessage("");
      return;
    }

    setIsLookupLoading(true);
    setErrorMessage("");

    try {
      const account = await lookupAccount(normalizedAccountNumber);
      setAccountName(account.accountName);
    } catch (error) {
      setAccountName("");
      if (error instanceof AxiosError) {
        setErrorMessage(
          error.response?.data?.message ??
            "Unable to resolve account name. Please verify account number.",
        );
      } else {
        setErrorMessage("Unable to resolve account name.");
      }
    } finally {
      setIsLookupLoading(false);
    }
  };

  const handleAccountLookup = async (e: React.FocusEvent<HTMLInputElement>) => {
    formik.handleBlur(e);
    await lookupAccountName(e.target.value);
  };

  useEffect(() => {
    const accountNumber = formik.values.accountNumber.trim();

    if (!accountNumber) {
      setAccountName("");
      setErrorMessage("");
      return;
    }

    const timeoutId = window.setTimeout(() => {
      void lookupAccountName(accountNumber);
    }, 450);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [formik.values.accountNumber]);

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
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-100"
        >
          <LogOut size={16} />
          Logout
        </button>
      </div>

      <section className="flex flex-col items-center justify-center">
        <h1 className="text-3xl font-bold mb-8 ">ESB Statement</h1>
        <h2 className="font-bold text-zinc-600">
          Welcome to your ESB Statement
        </h2>
        <p className="text-sm">Enter the details to print your statement</p>
      </section>

      <div className="flex items-center justify-center px-2 py-8">
        <form
          onSubmit={formik.handleSubmit}
          className="grid w-full max-w-2xl grid-cols-[200px_1fr] items-center gap-x-4 gap-y-10"
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
            onChange={formik.handleChange}
            onBlur={handleAccountLookup}
            required
            placeholder="Enter Account Number"
            className="w-full rounded border p-2"
          />

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
              required
              onKeyDown={blockManualDateTyping}
              onPaste={(e) => e.preventDefault()}
              className="w-full rounded border p-2 pr-10"
            />
            <CalendarDays
              size={16}
              className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-amber-500"
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
            <CalendarDays
              size={16}
              className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-amber-500"
            />
          </div>

          <div className="col-span-2 flex justify-center">
            <button
              type="submit"
              disabled={isPrintLoading}
              className="rounded bg-amber-400 px-4 py-2 text-white"
            >
              {isPrintLoading ? "Printing..." : "Print"}
            </button>
          </div>
        </form>
      </div>

      {errorMessage && (
        <p className="mb-4 text-center text-sm text-red-600">{errorMessage}</p>
      )}
    </main>
  );
};

export default ESBStatement;
