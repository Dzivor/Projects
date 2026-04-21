import { Route, Routes } from "react-router";
import Welcome from "../Components/Welcome";
import Login from "../Components/Login";
import Statement from "../Components/Statement";

const AppRoutes = () => {
  return (
    <Routes>
      <Route path="/" element={<Login />} />
      <Route path="/welcome" element={<Welcome />} />
      <Route path="/statement" element={<Statement />} />
    </Routes>
  );
};

export default AppRoutes;
