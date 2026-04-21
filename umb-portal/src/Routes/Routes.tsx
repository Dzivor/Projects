import { Route, Routes } from "react-router";
import Welcome from "../Components/Welcome";
import Login from "../Components/Login";
import Statement from "../Components/Statement";
import ESBStatement from "../Components/ESB-Statement";

const AppRoutes = () => {
  return (
    <Routes>
      <Route path="/" element={<Login />} />
      <Route path="/welcome" element={<Welcome />} />
      <Route path="/Statement" element={<Statement />} />
      <Route path="/ESB-Statement" element={<ESBStatement />} />
    </Routes>
  );
};

export default AppRoutes;
