import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ChevronDownIcon, ClipboardDocumentListIcon, ExclamationTriangleIcon } from "@heroicons/react/24/outline";
import StatusBadge from "../../components/order/StatusBadge";
import { ORDER_STATUS, ROUTES, type Order } from "../../utility/constants";
import { apiFetch, formatDate, formatPrice, getUser } from "../../utility/helperFunc";

const canCancel = (status: string) => status === ORDER_STATUS.CONFIRMED || status === ORDER_STATUS.READY;

function MyOrders() {
  const user = getUser()!; // RequireAuth guarantees a user
  const [orders, setOrders] = useState<Order[]>([]);
  const [status, setStatus] = useState<"loading" | "error" | "ready">("loading");
  const [error, setError] = useState("");
  const [reload, setReload] = useState(0);
  const [cancelling, setCancelling] = useState<number | null>(null);
  const userId = user.id;

  useEffect(() => {
    let active = true;
    // userId keeps an admin's list to their own orders; customers only ever get their own anyway.
    apiFetch<Order[]>(`/api/OrderHeader?userId=${encodeURIComponent(userId)}`)
      .then((data) => {
        if (!active) return;
        setOrders(data ?? []);
        setStatus("ready");
      })
      .catch((err: Error) => {
        if (!active) return;
        setError(err.message);
        setStatus("error");
      });
    return () => {
      active = false;
    };
  }, [userId, reload]);

  async function cancel(order: Order) {
    if (!window.confirm(`Cancel order #${order.orderHeaderId}? This can't be undone.`)) return;
    setCancelling(order.orderHeaderId);
    try {
      await apiFetch(`/api/OrderHeader/${order.orderHeaderId}`, "PUT", {
        orderHeaderId: order.orderHeaderId,
        status: ORDER_STATUS.CANCELLED,
      });
      setOrders((list) => list.map((o) => (o.orderHeaderId === order.orderHeaderId ? { ...o, status: ORDER_STATUS.CANCELLED } : o)));
    } catch (err) {
      window.alert((err as Error).message);
    } finally {
      setCancelling(null);
    }
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-12 sm:px-6">
      <h1 className="font-display text-4xl font-bold">My orders</h1>

      {status === "loading" && (
        <div className="mt-8 space-y-4" aria-busy="true">
          {[0, 1, 2].map((i) => (
            <div key={i} className="h-24 animate-pulse rounded-2xl border border-line bg-white" />
          ))}
        </div>
      )}

      {status === "error" && (
        <div role="alert" className="mt-8 flex flex-col items-start gap-4 rounded-2xl border border-line bg-white p-6">
          <p className="flex items-center gap-2 font-semibold">
            <ExclamationTriangleIcon className="h-5 w-5 text-primary" />
            {error}
          </p>
          <button
            type="button"
            onClick={() => {
              setStatus("loading");
              setReload((n) => n + 1);
            }}
            className="min-h-11 rounded-xl bg-primary px-5 font-semibold text-white transition-colors hover:bg-primary-hover"
          >
            Try again
          </button>
        </div>
      )}

      {status === "ready" && orders.length === 0 && (
        <div className="mt-12 flex flex-col items-center text-center">
          <ClipboardDocumentListIcon className="h-14 w-14 text-line" />
          <p className="mt-4 text-lg font-semibold">No orders yet</p>
          <p className="mt-1 text-ink/70">Your orders will show up here.</p>
          <Link
            to={`${ROUTES.HOME}#menu`}
            className="mt-6 inline-flex min-h-12 items-center rounded-xl bg-primary px-6 font-semibold text-white transition-colors hover:bg-primary-hover"
          >
            Browse the menu
          </Link>
        </div>
      )}

      {status === "ready" && orders.length > 0 && (
        <ul className="mt-8 space-y-4">
          {orders.map((order) => (
            <li key={order.orderHeaderId} className="rounded-2xl border border-line bg-white">
              <details className="group">
                <summary className="flex min-h-11 cursor-pointer list-none flex-wrap items-center gap-x-4 gap-y-2 p-5 [&::-webkit-details-marker]:hidden">
                  <div className="min-w-0 basis-full sm:basis-0 sm:flex-1">
                    <p className="font-display text-lg font-bold">Order #{order.orderHeaderId}</p>
                    <p className="text-sm text-ink/70">
                      {formatDate(new Date(order.orderDate))} · {order.totalItem} {order.totalItem === 1 ? "item" : "items"}
                    </p>
                  </div>
                  <StatusBadge status={order.status} />
                  <span className="ml-auto text-right font-bold tabular-nums text-accent sm:ml-0 sm:w-20">{formatPrice(order.orderTotal)}</span>
                  <ChevronDownIcon className="h-5 w-5 text-ink/60 transition-transform group-open:rotate-180" aria-hidden />
                </summary>

                <div className="border-t border-line p-5">
                  <ul className="space-y-2 text-sm">
                    {order.orderDetails.map((line) => (
                      <li key={line.orderDetailId} className="flex justify-between gap-3">
                        <span>
                          <span className="font-semibold tabular-nums">{line.quantity}×</span> {line.itemName}
                        </span>
                        <span className="tabular-nums">{formatPrice(line.price * line.quantity)}</span>
                      </li>
                    ))}
                  </ul>
                  {canCancel(order.status) && (
                    <button
                      type="button"
                      onClick={() => cancel(order)}
                      disabled={cancelling === order.orderHeaderId}
                      className="mt-5 min-h-11 rounded-xl border border-primary px-5 text-sm font-semibold text-primary transition-colors hover:bg-primary/5 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                      {cancelling === order.orderHeaderId ? "Cancelling…" : "Cancel order"}
                    </button>
                  )}
                </div>
              </details>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default MyOrders;
