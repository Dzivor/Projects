import { useFormik } from "formik";
import { AxiosError } from "axios";
import { useState } from "react";
import { useNavigate } from "react-router";
import { Lock, User, Eye, EyeOff } from "lucide-react";
import bgPattern from "../assets/Astek Patern-02.png";
import umbLogo from "../assets/umb-logo.jpg";
import { login } from "../services/auth";

const getFirstName = (fullName: string): string => {
  const firstName = fullName.trim().split(/\s+/)[0];

  return firstName || "Guest";
};

function Login() {
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  // Control the dev bypass explicitly with VITE_DEV_BYPASS (default: disabled)
  const isDevBypassEnabled = import.meta.env.VITE_DEV_BYPASS === "true";

  const formik = useFormik({
    initialValues: {
      username: "",
      password: "",
    },
    onSubmit: async (values) => {
      setErrorMessage("");
      setIsSubmitting(true);
      // In development mode, bypass actual login for faster testing

      if (isDevBypassEnabled) {
        const devUser = {
          token: "dev-bypass-token",
          username: values.username || "dev.user",
          fullName: "Developer",
          firstName: "Developer",
          expiresAt: new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString(),
        };

        localStorage.setItem("authToken", devUser.token ?? "");
        localStorage.setItem("authUser", JSON.stringify(devUser));

        navigate("/welcome");
        setIsSubmitting(false);
        return;
      }
      ///////////////////////////////////////////////////////////
      try {
        const result = await login(values);
        const authUser = {
          ...result,
          firstName: getFirstName(result.fullName ?? ""),
        };

        localStorage.setItem("authToken", result.token ?? "");
        localStorage.setItem("authUser", JSON.stringify(authUser));

        navigate("/welcome");
      } catch (error) {
        if (error instanceof AxiosError) {
          const apiMessage = error.response?.data?.message;
          setErrorMessage(apiMessage ?? "Login failed. Please try again.");
        } else {
          setErrorMessage("Login failed. Please try again.");
        }
      } finally {
        setIsSubmitting(false);
      }
    },
  });

  return (
    <main className="relative flex min-h-screen items-center justify-center px-4">
      <img
        src={bgPattern}
        alt=""
        aria-hidden="true"
        className="absolute inset-0 h-full w-full object-cover"
      />
      <div className="absolute inset-0 bg-slate-950/55" />

      <section className="relative z-10 w-full max-w-100 rounded-[18px] bg-[#ffffff] px-4 py-4 shadow-[0_10px_30px_rgba(0,0,0,0.28)]">
        <header className="mb-8 flex justify-center">
          <img src={umbLogo} alt="UMB Logo" className="h-25 w-28 py-1 px-2" />
        </header>

        <form onSubmit={formik.handleSubmit} className="space-y-10">
          <label className="flex h-12 items-center gap-4 rounded-full border border-[#bcc2c7] bg-[#ececec] px-6">
            <User size={15} className="text-[#697786]" />
            <input
              type="text"
              name="username"
              required
              placeholder="Username"
              value={formik.values.username}
              onChange={formik.handleChange}
              className="w-full bg-transparent text-[20px] font-normal text-[#2f3f53] placeholder:text-[#2f3f53]/90 focus:outline-none"
            />
          </label>

          <label className="flex h-12 items-center gap-4 rounded-full border border-[#bcc2c7] bg-[#ececec] px-4">
            <Lock size={15} className="text-[#697786]" />
            <input
              type={showPassword ? "text" : "password"}
              name="password"
              required
              placeholder="Password"
              value={formik.values.password}
              onChange={formik.handleChange}
              className="flex-1 bg-transparent text-[20px] font-normal text-[#2f3f53] placeholder:text-[#2f3f53]/90 focus:outline-none"
            />
            <button
              type="button"
              onClick={() => setShowPassword((s) => !s)}
              aria-label={showPassword ? "Hide password" : "Show password"}
              className="ml-2 flex h-8 w-8 items-center justify-center rounded-full text-[#697786] hover:bg-slate-200"
            >
              {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
          </label>

          <button
            type="submit"
            disabled={isSubmitting}
            className="h-12 w-full rounded-full bg-[#f3b21b] text-[20px] font-medium text-white transition hover:brightness-95"
          >
            {isSubmitting ? "Signing in..." : "Login"}
          </button>

          {errorMessage && (
            <p className="text-center text-sm text-red-600">{errorMessage}</p>
          )}
        </form>
      </section>
    </main>
  );
}

export default Login;
