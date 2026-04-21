import { LogOut } from "lucide-react";
import { useNavigate } from "react-router";
import { logoutUser } from "../services/session";

function Welcome() {
  const navigate = useNavigate();

  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top,#fff7e0_0%,#f3f7fb_45%,#eef3fa_100%)] px-4 py-10 sm:py-14">
      <div className="mb-6 flex justify-end">
        <button
          type="button"
          onClick={() => logoutUser(navigate)}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-100"
        >
          <LogOut size={14} />
          Logout
        </button>
      </div>
      <section className="mx-auto w-full max-w-[720px] mt-20 rounded-2xl border border-slate-200 bg-white p-6 shadow-[0_16px_35px_rgba(15,23,42,0.12)] sm:p-8">
        <div className="mb-8 flex items-start justify-between gap-4">
          <div>
            <h1 className="mt-2 text-2xl font-semibold text-slate-900 sm:text-3xl">
              Select Statement Type
            </h1>
            <p className="mt-2 text-sm text-slate-500">
              Choose the statement category to continue.
            </p>
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <button
            type="button"
            onClick={() => navigate("/Statement")}
            className="rounded-xl border border-[#d69f21] bg-[#f3b21b] px-6 py-4 text-base font-semibold text-[#1a1302] shadow-[inset_0_1px_0_rgba(255,255,255,0.35)] transition hover:brightness-95"
          >
            Visa Statement
          </button>

          <button
            type="button"
            onClick={() => navigate("/esb-statement")}
            className="rounded-xl border border-[#d69f21] bg-[#f8fafc] px-6 py-4 text-base font-semibold text-slate-900 transition hover:bg-[#fff7e3]"
          >
            ESB Statement
          </button>
        </div>
      </section>
    </main>
  );
}

export default Welcome;
