import { Link } from "react-router-dom";
import { ShoppingCartIcon, TrashIcon } from "@heroicons/react/24/outline";
import QuantityStepper from "../../components/ui/QuantityStepper";
import { setQuantity, useCart } from "../../store/cart";
import { ROUTES } from "../../utility/constants";
import { formatPrice, imageUrl } from "../../utility/helperFunc";

function Cart() {
  const lines = useCart();
  const itemCount = lines.reduce((n, l) => n + l.quantity, 0);
  const subtotal = lines.reduce((sum, l) => sum + l.item.price * l.quantity, 0);

  if (lines.length === 0) {
    return (
      <div className="mx-auto flex max-w-md flex-col items-center px-4 py-20 text-center">
        <ShoppingCartIcon className="h-14 w-14 text-line" />
        <h1 className="mt-4 font-display text-3xl font-bold">Your cart is empty</h1>
        <p className="mt-2 text-ink/70">Find something tasty on the menu.</p>
        <Link
          to={`${ROUTES.HOME}#menu`}
          className="mt-8 inline-flex min-h-12 items-center rounded-xl bg-primary px-6 font-semibold text-white transition-colors hover:bg-primary-hover"
        >
          Browse the menu
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
      <h1 className="font-display text-4xl font-bold">Your cart</h1>

      <div className="mt-8 grid gap-8 lg:grid-cols-[1fr_22rem] lg:items-start">
        <ul className="min-w-0 divide-y divide-line rounded-2xl border border-line bg-white">
          {lines.map(({ item, quantity }) => (
            <li key={item.id} className="flex gap-4 p-4 sm:p-5">
              <img
                src={imageUrl(item)}
                alt=""
                className="h-20 w-20 shrink-0 rounded-xl bg-line/40 object-cover sm:h-24 sm:w-24"
              />
              <div className="flex min-w-0 flex-1 flex-col gap-3 sm:flex-row sm:items-center">
                <div className="min-w-0 flex-1">
                  <h2 className="font-display text-lg font-bold">{item.name}</h2>
                  <p className="text-sm tabular-nums text-ink/70">{formatPrice(item.price)} each</p>
                </div>
                <div className="flex items-center justify-between gap-3 sm:justify-end">
                  <QuantityStepper quantity={quantity} onChange={(q) => setQuantity(item, q)} label={item.name} />
                  <p className="text-right font-bold tabular-nums text-accent sm:w-20">
                    {formatPrice(item.price * quantity)}
                  </p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setQuantity(item, 0)}
                aria-label={`Delete ${item.name} from cart`}
                className="-mr-2 -mt-2 flex h-11 w-11 shrink-0 items-center justify-center self-start rounded-full text-ink/60 transition-colors hover:bg-primary/10 hover:text-primary sm:m-0 sm:self-center"
              >
                <TrashIcon className="h-5 w-5" />
              </button>
            </li>
          ))}
        </ul>

        <aside className="rounded-2xl border border-line bg-white p-6 lg:sticky lg:top-24">
          <h2 className="font-display text-xl font-bold">Order summary</h2>
          <dl className="mt-4 space-y-2 text-sm">
            <div className="flex justify-between">
              <dt className="text-ink/70">Items</dt>
              <dd className="tabular-nums">{itemCount}</dd>
            </div>
            <div className="flex justify-between border-t border-line pt-3 text-base font-bold">
              <dt>Total</dt>
              <dd className="tabular-nums text-accent">{formatPrice(subtotal)}</dd>
            </div>
          </dl>
          <Link
            to={ROUTES.CHECKOUT}
            className="mt-6 flex min-h-12 items-center justify-center rounded-xl bg-primary font-semibold text-white transition-colors hover:bg-primary-hover"
          >
            Go to checkout
          </Link>
          <Link
            to={`${ROUTES.HOME}#menu`}
            className="mt-3 flex min-h-11 items-center justify-center text-sm font-semibold text-primary hover:underline"
          >
            Keep browsing
          </Link>
        </aside>
      </div>
    </div>
  );
}

export default Cart;
