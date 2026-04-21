import { useFormik } from "formik";
import { AxiosError } from "axios";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import { LogOut } from "lucide-react";
import { lookupAccount } from "../services/statement";
import { logoutUser } from "../services/session";

const VisaStatement = () => {
  const navigate = useNavigate();
  const [isLookupLoading, setIsLookupLoading] = useState(false);
  const [accountName, setAccountName] = useState("");
  const [lookupError, setLookupError] = useState("");

  const formik = useFormik({
    initialValues: {
      accountNumber: "",
      startDate: "",
      endDate: "",
      chargeAccNumber: "",
      chargeAltAccount: false,
      waiveCharge: false,
    },
    onSubmit: () => {
      // static phase: no auth call yet
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

  const handleChargeAccountNumberChange = (
    e: React.ChangeEvent<HTMLInputElement>,
  ) => {
    const normalizedAccountNumber = e.target.value
      .replace(/\D/g, "")
      .slice(0, 13);
    formik.setFieldValue("chargeAccNumber", normalizedAccountNumber);
  };

  const lookupAccountName = async (accountNumber: string) => {
    const normalizedAccountNumber = accountNumber.trim();

    if (!normalizedAccountNumber) {
      setAccountName("");
      setLookupError("");
      return;
    }

    if (normalizedAccountNumber.length < 13) {
      setAccountName("");
      setLookupError("");
      return;
    }

    setIsLookupLoading(true);
    setLookupError("");

    try {
      const account = await lookupAccount(normalizedAccountNumber);
      setAccountName(account.accountName);
    } catch (error) {
      if (error instanceof AxiosError && error.code === "ERR_CANCELED") {
        return;
      }

      setAccountName("");
      if (error instanceof AxiosError) {
        setLookupError(
          error.response?.data?.message ??
            "Unable to resolve account name. Please verify account number.",
        );
      } else {
        setLookupError("Unable to resolve account name.");
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
      setLookupError("");
      return;
    }

    if (accountNumber.length < 13) {
      setAccountName("");
      setLookupError("");
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
          onClick={() => logoutUser(navigate)}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-100"
        >
          <LogOut size={16} />
          Logout
        </button>
      </div>

      <section className="flex flex-col items-center justify-center">
        <h1 className="text-3xl font-bold mb-8 ">VISA Statement</h1>
        <h2 className="font-bold text-zinc-600">
          Welcome to your VISA Statement
        </h2>
        <p className="text-sm">Enter the details to preview charges</p>
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
            onChange={handlePrimaryAccountNumberChange}
            onBlur={handleAccountLookup}
            inputMode="numeric"
            maxLength={13}
            minLength={13}
            pattern="[0-9]{13}"
            required
            title="Account number must be exactly 13 digits"
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
            onChange={formik.handleChange}
            className="h-4 w-4 justify-self-start accent-amber-500"
          />

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
            htmlFor="chargeAccNumber"
            className="text-sm font-medium text-slate-700"
          >
            Charge Account Number:
          </label>
          <input
            id="chargeAccNumber"
            name="chargeAccNumber"
            value={formik.values.chargeAccNumber}
            onChange={handleChargeAccountNumberChange}
            inputMode="numeric"
            maxLength={13}
            minLength={13}
            pattern="[0-9]{13}"
            title="Charge account number must be exactly 13 digits"
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

          <div />
          <button
            type="submit"
            className="justify-self-start rounded bg-amber-400 px-4 py-2 text-white"
          >
            Preview Charges
          </button>
        </form>
      </div>

      {lookupError && (
        <p className="mb-4 text-center text-sm text-red-600">{lookupError}</p>
      )}
    </main>
  );
};

export default VisaStatement;
