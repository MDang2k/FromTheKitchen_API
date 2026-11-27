import { MinusIcon, PlusIcon } from "@heroicons/react/24/outline";
import { MAX_QUANTITY } from "../../store/cart";

interface QuantityStepperProps {
  quantity: number;
  onChange: (quantity: number) => void;
  label: string;
}

const btn =
  "flex h-11 w-11 items-center justify-center rounded-full text-ink transition-colors hover:bg-primary/10 hover:text-primary disabled:cursor-not-allowed disabled:opacity-40";

function QuantityStepper({ quantity, onChange, label }: QuantityStepperProps) {
  return (
    <div className="inline-flex items-center rounded-full border border-line bg-white" role="group" aria-label={`Quantity of ${label}`}>
      <button type="button" className={btn} onClick={() => onChange(quantity - 1)} aria-label={quantity === 1 ? `Remove ${label}` : `One less ${label}`}>
        <MinusIcon className="h-4 w-4" />
      </button>
      <span className="w-6 text-center font-semibold tabular-nums" aria-live="polite">
        {quantity}
      </span>
      <button
        type="button"
        className={btn}
        onClick={() => onChange(quantity + 1)}
        disabled={quantity >= MAX_QUANTITY}
        aria-label={`One more ${label}`}
      >
        <PlusIcon className="h-4 w-4" />
      </button>
    </div>
  );
}

export default QuantityStepper;
