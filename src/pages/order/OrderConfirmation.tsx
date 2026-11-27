import { Link, Navigate, useLocation } from "react-router-dom";
import { CheckCircleIcon } from "@heroicons/react/24/outline";
import type { PlacedOrder } from "../cart/Checkout";
import { ROUTES } from "../../utility/constants";
import { formatPrice } from "../../utility/helperFunc";

// Shows the order just placed; without one (refresh, direct link) the order list is the right place.
function OrderConfirmation() {
  const order = (useLocation().state as { order?: PlacedOrder } | null)?.order;

  if (!order) return <Navigate to={ROUTES.MY_ORDERS} replace />;

  return (
    <div className="mx-auto flex max-w-md flex-col items-center px-4 py-20 text-center">
      <CheckCircleIcon className="h-16 w-16 text-green-700" />
      <h1 className="mt-4 font-display text-4xl font-bold">Order confirmed</h1>
      <p className="mt-2 text-ink/70">
        Thanks, {order.pickUpName}. We're on it and will let you know when it's ready for pick-up.
      </p>
      <dl className="mt-8 w-full space-y-2 rounded-2xl border border-line bg-white p-6 text-left text-sm">
        <div className="flex justify-between">
          <dt className="text-ink/70">Order number</dt>
          <dd className="font-semibold tabular-nums">#{order.orderHeaderId}</dd>
        </div>
        <div className="flex justify-between">
          <dt className="text-ink/70">Items</dt>
          <dd className="tabular-nums">{order.totalItem}</dd>
        </div>
        <div className="flex justify-between">
          <dt className="text-ink/70">Status</dt>
          <dd>{order.status}</dd>
        </div>
        <div className="flex justify-between border-t border-line pt-3 text-base font-bold">
          <dt>Total</dt>
          <dd className="tabular-nums text-accent">{formatPrice(order.orderTotal)}</dd>
        </div>
      </dl>
      <div className="mt-8 flex flex-wrap justify-center gap-3">
        <Link
          to={ROUTES.MY_ORDERS}
          className="inline-flex min-h-12 items-center rounded-xl bg-primary px-6 font-semibold text-white transition-colors hover:bg-primary-hover"
        >
          View my orders
        </Link>
        <Link
          to={ROUTES.HOME}
          className="inline-flex min-h-12 items-center rounded-xl border border-line bg-white px-6 font-semibold transition-colors hover:border-primary hover:text-primary"
        >
          Back to the menu
        </Link>
      </div>
    </div>
  );
}

export default OrderConfirmation;
