import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  ChevronDown,
  CreditCard,
  LogOut,
  File,
  type LucideIcon,
} from "lucide-react";
import { logoutUser } from "../services/session";

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

type StatementType = {
  id: string;
  title: string;
  description: string;
  icon: LucideIcon;
  path: string;
};

const statementTypes: StatementType[] = [
  {
    id: "visa",
    title: "VISA  STATEMENT",
    description: "Transaction history and statement details for Visa channels.",
    icon: CreditCard,
    path: "/Statement",
  },
  {
    id: "esb",
    title: "ESB  STATEMENT",
    description: "Statement details and balances for ESB channel requests.",
    icon: File,
    path: "/ESB-Statement",
  },
];

const getInitials = (name: string): string => {
  const pieces = name.trim().split(/\s+/).filter(Boolean);

  if (pieces.length === 0) {
    return "G";
  }

  return pieces
    .map((piece) => piece[0]?.toUpperCase() ?? "")
    .join("")
    .slice(0, 2);
};

function Welcome() {
  const navigate = useNavigate();
  const [selectedStatementType, setSelectedStatementType] = useState<
    string | null
  >(null);
  const [isAccountMenuOpen, setIsAccountMenuOpen] = useState(false);
  const accountMenuRef = useRef<HTMLDivElement | null>(null);
  const userName = getWelcomeName();

  useEffect(() => {
    if (!isAccountMenuOpen) {
      return;
    }

    const handlePointerDown = (event: PointerEvent) => {
      const target = event.target as Node;

      if (accountMenuRef.current && !accountMenuRef.current.contains(target)) {
        setIsAccountMenuOpen(false);
      }
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsAccountMenuOpen(false);
      }
    };

    document.addEventListener("pointerdown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [isAccountMenuOpen]);

  const accountInitials = getInitials(userName);

  const handleStatementSelect = (statementType: StatementType) => {
    setSelectedStatementType(statementType.id);
    window.requestAnimationFrame(() => {
      navigate(statementType.path);
    });
  };

  return (
    <main className="relative min-h-screen overflow-hidden bg-[#f3f4f6] px-4 py-6 sm:px-6 lg:px-8">
      <div className="pointer-events-none absolute inset-x-0 top-0 h-56 bg-[radial-gradient(circle_at_top,rgba(239,159,39,0.18)_0%,rgba(239,159,39,0.08)_26%,rgba(243,244,246,0)_68%)]" />

      <div className="relative mx-auto flex min-h-[calc(100vh-3rem)] w-full max-w-5xl items-center justify-center">
        <div className="absolute right-0 top-0 z-20" ref={accountMenuRef}>
          <button
            type="button"
            aria-haspopup="menu"
            aria-label="Account menu"
            onClick={() => setIsAccountMenuOpen((current) => !current)}
            className="flex items-center gap-3 rounded-full border border-slate-200 bg-white px-3 py-2 text-left shadow-[0_8px_20px_rgba(15,23,42,0.08)] transition hover:-translate-y-0.5 hover:border-amber-300 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-amber-100"
          >
            <span className="flex h-10 w-10 items-center justify-center rounded-full bg-[#ef9f27] text-sm font-semibold text-[#1f1605] shadow-[inset_0_1px_0_rgba(255,255,255,0.35)]">
              {accountInitials}
            </span>
            <ChevronDown size={16} className="text-slate-500" />
          </button>

          {isAccountMenuOpen ? (
            <div
              role="menu"
              aria-label="Account menu"
              className="absolute right-0 mt-2 w-48 rounded-2xl border border-slate-200 bg-white p-2 shadow-[0_20px_40px_rgba(15,23,42,0.12)]"
            >
              <button
                type="button"
                role="menuitem"
                onClick={() => logoutUser(navigate)}
                className="flex w-full items-center gap-3 rounded-xl px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100 hover:text-slate-900 focus-visible:bg-slate-100 focus-visible:outline-none"
              >
                <LogOut size={16} className="text-slate-500" />
                Log out
              </button>
            </div>
          ) : null}
        </div>

        <section className="w-full rounded-[28px] border border-slate-200 bg-white/95 px-5 py-6 shadow-[0_20px_50px_rgba(15,23,42,0.08)] backdrop-blur-sm sm:px-8 sm:py-8 lg:px-10 lg:py-10">
          <div className="mb-8 max-w-2xl">
            <p className="text-sm font-medium text-slate-500">
              Welcome back, {userName}
            </p>
            <h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-900 sm:text-4xl">
              Select statement type
            </h1>
            <p className="mt-3 text-sm leading-6 text-slate-500 sm:text-base">
              Choose the channel you want to use to open a statement.
            </p>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            {statementTypes.map((statementType) => {
              const Icon = statementType.icon;
              const isSelected = selectedStatementType === statementType.id;

              return (
                <button
                  key={statementType.id}
                  type="button"
                  onClick={() => handleStatementSelect(statementType)}
                  onFocus={() => setSelectedStatementType(statementType.id)}
                  className={`group flex h-full min-h-44 flex-col items-start rounded-2xl border-2 bg-white p-5 text-left transition duration-200 hover:-translate-y-1 hover:shadow-[0_18px_36px_rgba(15,23,42,0.08)] focus-visible:-translate-y-1 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-amber-100 active:translate-y-0.5 ${
                    isSelected
                      ? "border-amber-500 shadow-[0_18px_36px_rgba(239,159,39,0.16)]"
                      : "border-slate-200 hover:border-amber-300 focus-visible:border-amber-500"
                  }`}
                >
                  <span
                    className={`flex h-12 w-12 items-center justify-center rounded-2xl border transition ${
                      isSelected
                        ? "border-amber-500 bg-amber-50 text-[#ef9f27]"
                        : "border-slate-200 bg-slate-50 text-slate-700 group-hover:border-amber-300 group-hover:bg-amber-50 group-hover:text-[#ef9f27]"
                    }`}
                    aria-hidden="true"
                  >
                    <Icon size={22} />
                  </span>

                  <div className="mt-4">
                    <h2 className="text-lg font-semibold text-slate-900">
                      {statementType.title}
                    </h2>
                    <p className="mt-2 max-w-sm text-sm leading-6 text-slate-500">
                      {statementType.description}
                    </p>
                  </div>
                </button>
              );
            })}
          </div>
        </section>
      </div>
    </main>
  );
}

export default Welcome;
