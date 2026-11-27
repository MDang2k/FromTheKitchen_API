import { ORDER_STATUS } from "../../utility/constants";

// Text always carries the meaning; color only reinforces it.
const STYLES: Record<string, string> = {
  [ORDER_STATUS.CONFIRMED]: "bg-amber-100 text-amber-900",
  [ORDER_STATUS.READY]: "bg-green-100 text-green-900",
  [ORDER_STATUS.COMPLETED]: "bg-gray-100 text-gray-700",
  [ORDER_STATUS.CANCELLED]: "bg-red-100 text-red-900",
};

function StatusBadge({ status }: { status: string }) {
  return (
    <span className={`whitespace-nowrap rounded-full px-3 py-1 text-xs font-semibold ${STYLES[status] ?? "bg-gray-100"}`}>
      {status}
    </span>
  );
}

export default StatusBadge;
