import { useFormik } from "formik";
import { AxiosError } from "axios";
import { useState } from "react";
import { useNavigate } from "react-router";
import { Lock, User } from "lucide-react";
import bgPattern from "../assets/Astek Patern-02.png";
import umbLogo from "../assets/umb-logo.jpg";
import { login } from "../services/auth";

function Login() {
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const formik = useFormik({
    initialValues: {
      username: "",
      password: "",
    },
    onSubmit: async (values) => {
      setErrorMessage("");
      setIsSubmitting(true);

      try {
        const result = await login(values);

        localStorage.setItem("authToken", result.token);
        localStorage.setItem("authUser", JSON.stringify(result));

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
    <main
      className="flex min-h-screen items-center justify-center px-4"
      style={{
        backgroundImage: `url(${bgPattern})`,
        backgroundSize: "cover",
        backgroundPosition: "center",
      }}
    >
      <section className="w-full max-w-100 rounded-[18px] bg-[#ffffff] px-4 py-4 shadow-[0_10px_30px_rgba(0,0,0,0.28)]">
        <header className="mb-8 flex justify-center">
          <img src={umbLogo} alt="UMB Logo" className="h-25 w-28 py-1 px-2" />
        </header>

        <form onSubmit={formik.handleSubmit} className="space-y-10">
          <label className="flex h-12 items-center gap-4 rounded-full border border-[#bcc2c7] bg-[#ececec] px-6">
            <User size={15} className="text-[#697786]" />
            <input
              type="email"
              name="username"
              required
              placeholder="Username"
              value={formik.values.username}
              onChange={formik.handleChange}
              className="w-full bg-transparent text-[20px] font-normal text-[#2f3f53] placeholder:text-[#2f3f53]/90 focus:outline-none"
            />
          </label>

          <label className="flex h-12 items-center gap-4 rounded-full border border-[#bcc2c7] bg-[#ececec] px-6">
            <Lock size={15} className="text-[#697786]" />
            <input
              type="password"
              name="password"
              required
              placeholder="Password"
              value={formik.values.password}
              onChange={formik.handleChange}
              className="w-full bg-transparent text-[20px] font-normal text-[#2f3f53] placeholder:text-[#2f3f53]/90 focus:outline-none"
            />
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
