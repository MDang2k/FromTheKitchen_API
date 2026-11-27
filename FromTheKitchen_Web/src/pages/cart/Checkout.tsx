import { useState, type FormEvent } from "react";
import { Link, Navigate, useNavigate } from "react-router-dom";
import { ExclamationCircleIcon } from "@heroicons/react/24/outline";
import Field from "../../components/ui/Field";
import { clearCart, useCart } from "../../store/cart";
import { ROUTES } from "../../utility/constants";
import { formatPrice, getUser, apiFetch } from "../../utility/helperFunc";

export interface PlacedOrder {
  orderHeaderId: number;
  pickUpName: string;
  orderTotal: number;
  totalItem: number;
  status: string;
}

function Checkout() {
  const navigate = useNavigate();
  const lines = useCart();
  const user = getUser()!; // RequireAuth guarantees a user
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  if (lines.length === 0 && !loading) return <Navigate to={ROUTES.CART} replace />;

  const itemCount = lines.reduce((n, l) => n + l.quantity, 0);
  const total = lines.reduce((sum, l) => sum + l.item.price * l.quantity, 0);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    setError("");
    setLoading(true);
    try {
      // The API prices the order itself; only ids and quantities matter here.
      const order = await apiFetch<PlacedOrder>("/api/OrderHeader", "POST", {
        pickUpName: form.get("pickUpName"),
        phoneNumber: form.get("phoneNumber"),
        pickUpEmail: form.get("pickUpEmail"),
        orderDetailsDTO: lines.map((l) => ({ menuItemId: l.item.id, quantity: l.quantity })),
      });
      navigate(ROUTES.ORDER_CONFIRMATION, { state: { order }, replace: true });
      clearCart();
    } catch (err) {
      setError((err as Error).message);
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
      <h1 className="font-display text-4xl font-bold">Checkout</h1>
      <p className="mt-2 text-ink/70">Pick-up only. We'll let you know when your order is ready.</p>

      <div className="mt-8 grid gap-8 lg:grid-cols-[1fr_22rem] lg:items-start">
        <form onSubmit={handleSubmit} className="min-w-0 space-y-5 rounded-2xl border border-line bg-white p-6" id="checkout">
          <h2 className="font-display text-xl font-bold">Pick-up details</h2>

          {error && (
            <p role="alert" className="flex items-start gap-2 whitespace-pre-line rounded-xl bg-red-50 p-4 text-sm font-semibold text-primary-hover">
              <ExclamationCircleIcon className="h-5 w-5 shrink-0" />
              {error}
            </p>
          )}

          <Field label="Name" name="pickUpName" autoComplete="name" defaultValue={user.name !== user.email ? user.name : ""} required />
          <Field label="Phone number" name="phoneNumber" type="tel" autoComplete="tel" minLength={7} required hint="We'll call if there's a problem with your order." />
          <Field label="Email" name="pickUpEmail" type="email" autoComplete="email" defaultValue={user.email} required />
        </form>

        <aside className="rounded-2xl border border-line bg-white p-6 lg:sticky lg:top-24">
          <h2 className="font-display text-xl font-bold">Your order</h2>
          <ul className="mt-4 space-y-3 text-sm">
            {lines.map(({ item, quantity }) => (
              <li key={item.id} className="flex justify-between gap-3">
                <span className="min-w-0">
                  <span className="font-semibold tabular-nums">{quantity}×</span> {item.name}
                </span>
                <span className="tabular-nums">{formatPrice(item.price * quantity)}</span>
              </li>
            ))}
          </ul>
          <div className="mt-4 flex justify-between border-t border-line pt-3 text-base font-bold">
            <span>Total ({itemCount} items)</span>
            <span className="tabular-nums text-accent">{formatPrice(total)}</span>
          </div>
          <p className="mt-2 text-xs text-ink/60">Pay when you pick up. The final price is confirmed by the kitchen.</p>
          <button
            type="submit"
            form="checkout"
            disabled={loading}
            className="mt-6 min-h-12 w-full rounded-xl bg-primary font-semibold text-white transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
          >
            {loading ? "Placing order…" : "Place order"}
          </button>
          <Link to={ROUTES.CART} className="mt-3 flex min-h-11 items-center justify-center text-sm font-semibold text-primary hover:underline">
            Back to cart
          </Link>
        </aside>
      </div>
    </div>
  );
}

export default Checkout;
