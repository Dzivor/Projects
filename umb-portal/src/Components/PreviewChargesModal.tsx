type PreviewChargesModalProps = {
  isOpen: boolean;
  message: string;
  isPrinting: boolean;
  onPrint: () => void;
  onCancel: () => void;
};

const PreviewChargesModal = ({
  isOpen,
  message,
  isPrinting,
  onPrint,
  onCancel,
}: PreviewChargesModalProps) => {
  if (!isOpen) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
        <h3 className="text-xl font-semibold text-slate-900">Preview Charge</h3>
        <p className="mt-3 text-sm text-slate-700">{message}</p>

        <div className="mt-6 flex justify-end gap-3">
          <button
            type="button"
            onClick={onCancel}
            disabled={isPrinting}
            className="rounded bg-red-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={onPrint}
            disabled={isPrinting}
            className="rounded bg-green-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-green-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {isPrinting ? "Printing..." : "Print"}
          </button>
        </div>
      </div>
    </div>
  );
};

export default PreviewChargesModal;
