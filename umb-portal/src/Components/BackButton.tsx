import { ArrowLeft } from "lucide-react";
import { useNavigate } from "react-router-dom";

type BackButtonProps = {
  to?: string;
};

const BackButton = ({ to = "/welcome" }: BackButtonProps) => {
  const navigate = useNavigate();

  return (
    <button
      type="button"
      onClick={() => navigate(to)}
      className="inline-flex items-center gap-2 rounded-full border border-[#E6A817] px-4 py-2 text-sm font-medium text-[#E6A817] transition hover:bg-[#E6A817]/10 focus:outline-none focus:ring-2 focus:ring-[#E6A817]/40"
    >
      <ArrowLeft size={16} />
      Back
    </button>
  );
};

export default BackButton;
