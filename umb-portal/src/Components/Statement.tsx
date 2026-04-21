import { useFormik } from "formik";
import { CalendarDays, LogOut } from "lucide-react";

const EsbStatement = () => {
  const formik = useFormik({
    initialValues: {
      accountNumber: Number(),
      startDate: Date(),
      endDate: Date(),
      chargeAccNumber: "",
      chargeAltAccount: false,
      waiveCharge: false,
    },
    onSubmit: () => {
      // static phase: no auth call yet
    },
  });

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
        <h2 className="font-bold text-zinc-600">Welcome</h2>
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
            onChange={formik.handleChange}
            placeholder="Enter Account Number"
            className="w-full rounded border p-2"
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
            <CalendarDays
              size={16}
              className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-amber-500"
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
            <CalendarDays
              size={16}
              className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-amber-500"
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
            onChange={formik.handleChange}
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
    </main>
  );
};

export default EsbStatement;
