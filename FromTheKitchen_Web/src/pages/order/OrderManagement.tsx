import { useEffect, useState } from "react";
import { ArrowPathIcon, ChevronDownIcon, ExclamationTriangleIcon, MagnifyingGlassIcon } from "@heroicons/react/24/outline";
import StatusBadge from "../../components/order/StatusBadge";
import { ORDER_STATUS, type Order } from "../../utility/constants";
import { apiFetch, formatDate, formatPrice } from "../../utility/helperFunc";

const { CONFIRMED, READY, COMPLETED, CANCELLED } = ORDER_STATUS;
const REFRESH_MS = 30_000;

// The API allows only these forward moves; cancelling is handled separately.
const NEXT_STEP: Record<string, { to: string; label: string } | undefined> = {
  [CONFIRMED]: { to: READY, label: "Mark ready for pick-up" },
  [READY]: { to: COMPLETED, label: "Mark completed" },
};

const TABS = [
  { key: "active", label: "Active", match: (s: string) => s === CONFIRMED || s === READY },
  { key: "completed", label: "Completed", match: (s: string) => s === COMPLETED },
  { key: "cancelled", label: "Cancelled", match: (s: string) => s === CANCELLED },
  { key: "all", label: "All", match: () => true },
];

function OrderManagement() {
  const [orders, setOrders] = useState<Order[]>([]);
  const [status, setStatus] = useState<"loading" | "error" | "ready">("loading");
  const [error, setError] = useState("");
  const [updatedAt, setUpdatedAt] = useState<Date | null>(null);
  const [reload, setReload] = useState(0);
  const [tab, setTab] = useState("active");
  const [query, setQuery] = useState("");
  const [busy, setBusy] = useState<number | null>(null);

  // Load all orders, then refresh every 30s so new orders show up without a reload.
  useEffect(() => {
    let active = true;
    const load = () =>
      apiFetch<Order[]>("/api/OrderHeader")
        .then((data) => {
          if (!active) return;
          setOrders(data ?? []);
          setUpdatedAt(new Date());
          setStatus("ready");
        })
        .catch((err: Error) => {
          if (!active) return;
          setError(err.message);
          setStatus("error");
        });
    load();
    const timer = setInterval(load, REFRESH_MS);
    return () => {
      active = false;
      clearInterval(timer);
    };
  }, [reload]);

  async function changeStatus(order: Order, to: string) {
    if (to === CANCELLED && !window.confirm(`Cancel order #${order.orderHeaderId} for ${order.pickUpName}? This can't be undone.`)) return;
    setBusy(order.orderHeaderId);
    try {
      await apiFetch(`/api/OrderHeader/${order.orderHeaderId}`, "PUT", { orderHeaderId: order.orderHeaderId, status: to });
      setOrders((list) => list.map((o) => (o.orderHeaderId === order.orderHeaderId ? { ...o, status: to } : o)));
    } catch (err) {
      window.alert((err as Error).message);
    } finally {
      setBusy(null);
    }
  }

  const q = query.trim().toLowerCase();
  const matchesQuery = (o: Order) =>
    !q ||
    `#${o.orderHeaderId} ${o.orderHeaderId} ${o.pickUpName} ${o.phoneNumber} ${o.pickUpEmail}`.toLowerCase().includes(q);
  const searched = orders.filter(matchesQuery);
  const currentTab = TABS.find((t) => t.key === tab)!;
  const visible = searched.filter((o) => currentTab.match(o.status));

  return (
    <div className="mx-auto max-w-5xl px-4 py-12 sm:px-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="font-display text-4xl font-bold">Order management</h1>
          <p className="mt-1 text-sm text-ink/60" aria-live="polite">
            {updatedAt ? `Updated ${updatedAt.toLocaleTimeString()} · refreshes every 30 seconds` : "Loading orders…"}
          </p>
        </div>
        <button
          type="button"
          onClick={() => setReload((n) => n + 1)}
          className="inline-flex min-h-11 items-center gap-2 rounded-xl border border-line bg-white px-4 text-sm font-semibold transition-colors hover:border-primary hover:text-primary"
        >
          <ArrowPathIcon className="h-5 w-5" />
          Refresh
        </button>
      </div>

      <div className="mt-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-wrap gap-2" role="group" aria-label="Filter by status">
          {TABS.map((t) => (
            <button
              key={t.key}
              type="button"
              onClick={() => setTab(t.key)}
              aria-pressed={tab === t.key}
              className={`min-h-11 rounded-full border px-4 text-sm font-semibold transition-colors ${
                tab === t.key ? "border-primary bg-primary text-white" : "border-line bg-white hover:border-primary hover:text-primary"
              }`}
            >
              {t.label} <span className="tabular-nums opacity-80">({searched.filter((o) => t.match(o.status)).length})</span>
            </button>
          ))}
        </div>
        <label className="relative block sm:w-72">
          <span className="sr-only">Search orders</span>
          <MagnifyingGlassIcon className="pointer-events-none absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-ink/50" />
          <input
            type="search"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Order #, name, phone, email"
            className="min-h-11 w-full rounded-xl border border-line bg-white pl-10 pr-3 text-base focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          />
        </label>
      </div>

      {status === "loading" && (
        <div className="mt-6 space-y-3" aria-busy="true">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-20 animate-pulse rounded-2xl border border-line bg-white" />
          ))}
        </div>
      )}

      {status === "error" && (
        <div role="alert" className="mt-6 flex flex-col items-start gap-4 rounded-2xl border border-line bg-white p-6">
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

      {status === "ready" && visible.length === 0 && (
        <p className="mt-10 text-center text-ink/70">
          {q ? "No orders match your search." : `No ${currentTab.label.toLowerCase()} orders.`}
        </p>
      )}

      {status === "ready" && visible.length > 0 && (
        <ul className="mt-6 space-y-3">
          {visible.map((order) => {
            const next = NEXT_STEP[order.status];
            const canCancel = order.status === CONFIRMED || order.status === READY;
            return (
              <li key={order.orderHeaderId} className="rounded-2xl border border-line bg-white">
                <details className="group">
                  <summary className="flex min-h-11 cursor-pointer list-none flex-wrap items-center gap-x-4 gap-y-2 p-5 [&::-webkit-details-marker]:hidden">
                    <div className="min-w-0 basis-full sm:basis-0 sm:flex-1">
                      <p className="font-display text-lg font-bold">
                        #{order.orderHeaderId} · {order.pickUpName}
                      </p>
                      <p className="text-sm text-ink/70">
                        {formatDate(new Date(order.orderDate))} · {order.totalItem} {order.totalItem === 1 ? "item" : "items"}
                      </p>
                    </div>
                    <StatusBadge status={order.status} />
                    <span className="ml-auto text-right font-bold tabular-nums text-accent sm:ml-0 sm:w-20">
                      {formatPrice(order.orderTotal)}
                    </span>
                    <ChevronDownIcon className="h-5 w-5 text-ink/60 transition-transform group-open:rotate-180" aria-hidden />
                  </summary>

                  <div className="grid gap-6 border-t border-line p-5 sm:grid-cols-2">
                    <div>
                      <h2 className="text-xs font-semibold uppercase tracking-wide text-ink/60">Items</h2>
                      <ul className="mt-2 space-y-1.5 text-sm">
                        {order.orderDetails.map((line) => (
                          <li key={line.orderDetailId} className="flex justify-between gap-3">
                            <span>
                              <span className="font-semibold tabular-nums">{line.quantity}×</span> {line.itemName}
                            </span>
                            <span className="tabular-nums">{formatPrice(line.price * line.quantity)}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                    <div>
                      <h2 className="text-xs font-semibold uppercase tracking-wide text-ink/60">Customer</h2>
                      <dl className="mt-2 space-y-1.5 text-sm">
                        <div>
                          <dt className="sr-only">Phone</dt>
                          <dd>
                            <a href={`tel:${order.phoneNumber}`} className="font-semibold text-primary hover:underline">
                              {order.phoneNumber}
                            </a>
                          </dd>
                        </div>
                        <div>
                          <dt className="sr-only">Email</dt>
                          <dd className="break-all">
                            <a href={`mailto:${order.pickUpEmail}`} className="hover:underline">
                              {order.pickUpEmail}
                            </a>
                          </dd>
                        </div>
                      </dl>
                    </div>

                    {(next || canCancel) && (
                      <div className="flex flex-wrap gap-3 sm:col-span-2">
                        {next && (
                          <button
                            type="button"
                            onClick={() => changeStatus(order, next.to)}
                            disabled={busy === order.orderHeaderId}
                            className="min-h-11 rounded-xl bg-primary px-5 text-sm font-semibold text-white transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            {busy === order.orderHeaderId ? "Saving…" : next.label}
                          </button>
                        )}
                        {canCancel && (
                          <button
                            type="button"
                            onClick={() => changeStatus(order, CANCELLED)}
                            disabled={busy === order.orderHeaderId}
                            className="min-h-11 rounded-xl border border-primary px-5 text-sm font-semibold text-primary transition-colors hover:bg-primary/5 disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            Cancel order
                          </button>
                        )}
                      </div>
                    )}
                  </div>
                </details>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}

export default OrderManagement;
