import { Route, Routes } from "react-router";
import Welcome from "../Components/Welcome";
import Login from "../Components/Login";
import EsbStatement from "../Components/EsbStatement";

const AppRoutes = () => {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/welcome" element={<Welcome />} />
      <Route path="/esb_statement" element={<EsbStatement />} />
    </Routes>
  );
};

export default AppRoutes;
