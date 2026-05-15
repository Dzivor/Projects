import React from "react";

type Props = {
  isOpen: boolean;
  title?: string;
  message: string;
  onClose: () => void;
};

const ErrorModal: React.FC<Props> = ({
  isOpen,
  title = "Error",
  message,
  onClose,
}) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-lg rounded-lg bg-white p-6 shadow-lg">
        <h3 className="mb-2 text-lg font-semibold text-red-700">{title}</h3>
        <p className="mb-4 whitespace-pre-wrap text-sm text-slate-700">
          {message}
        </p>
        <div className="flex justify-end">
          <button
            onClick={onClose}
            className="rounded bg-amber-400 px-4 py-2 text-white hover:bg-amber-500"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default ErrorModal;
