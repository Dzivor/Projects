import { useFormik } from "formik";
import { Lock, User } from "lucide-react";
import bgPattern from "../assets/Astek Patern-02.png";
import umbLogo from "../assets/umb-logo.jpg";

function Login() {
  const formik = useFormik({
    initialValues: {
      username: "",
      password: "",
    },
    onSubmit: () => {
      // static phase: no auth call yet
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
      <section className="w-full max-w-100 rounded-[18px] bg-[#ffffff] px-8 py-8 shadow-[0_10px_30px_rgba(0,0,0,0.28)]">
        <header className="mb-8 flex justify-center">
          <img src={umbLogo} alt="UMB Logo" className="h-25 w-28 py-1 px-2" />
        </header>

        <form onSubmit={formik.handleSubmit} className="space-y-12">
          <label className="flex h-12 items-center gap-4 rounded-full border border-[#bcc2c7] bg-[#ececec] px-6">
            <User size={15} className="text-[#697786]" />
            <input
              type="text"
              name="username"
              required
              placeholder="Username"
              value={formik.values.username}
              onChange={formik.handleChange}
              className="w-full bg-transparent text-[30px] font-normal text-[#2f3f53] placeholder:text-[#2f3f53]/90 focus:outline-none"
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
              className="w-full bg-transparent text-[30px] font-normal text-[#2f3f53] placeholder:text-[#2f3f53]/90 focus:outline-none"
            />
          </label>

          <button
            type="submit"
            className="h-13 w-full rounded-full bg-[#f3b21b] text-[30px] font-medium text-white transition hover:brightness-95"
          >
            Login
          </button>
        </form>
      </section>
    </main>
  );
}

export default Login;
