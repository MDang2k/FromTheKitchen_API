import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";
import { ArrowDownIcon, ExclamationTriangleIcon, PlusIcon } from "@heroicons/react/24/outline";
import { API_BASE_URL, type MenuItem } from "../utility/constants";
import { formatPrice, imageUrl } from "../utility/helperFunc";
import { setQuantity, useCart } from "../store/cart";
import QuantityStepper from "../components/ui/QuantityStepper";

function Home() {
  const cart = useCart();
  const inCart = new Map(cart.map((l) => [l.item.id, l.quantity]));
  const { hash } = useLocation();

  // Router navigation doesn't scroll to #menu on its own.
  useEffect(() => {
    if (hash) document.querySelector(hash)?.scrollIntoView();
  }, [hash]);
  const [items, setItems] = useState<MenuItem[]>([]);
  const [status, setStatus] = useState<"loading" | "error" | "ready">("loading");
  const [category, setCategory] = useState("All");
  const [reload, setReload] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    fetch(`${API_BASE_URL}/api/MenuItem`, { signal: controller.signal })
      .then((res) => (res.ok ? res.json() : Promise.reject(res.status)))
      .then((data) => {
        setItems(data.result ?? []);
        setStatus("ready");
      })
      .catch(() => {
        if (!controller.signal.aborted) setStatus("error");
      });
    return () => controller.abort();
  }, [reload]);

  const categories = ["All", ...new Set(items.map((i) => i.category))];
  const visible = category === "All" ? items : items.filter((i) => i.category === category);
  const featured = items.find((i) => i.specialTag) ?? items[0];

  return (
    <>
      {/* Hero */}
      <section className="mx-auto grid max-w-7xl items-center gap-10 px-4 py-12 sm:px-6 md:grid-cols-2 md:py-20 lg:px-8">
        <div>
          <p className="text-sm font-semibold uppercase tracking-widest text-accent">
            Cooked fresh, every day
          </p>
          <h1 className="mt-3 font-display text-4xl font-bold leading-tight sm:text-5xl lg:text-6xl">
            Home cooking, <span className="text-primary">straight from our kitchen.</span>
          </h1>
          <p className="mt-5 max-w-md text-lg leading-relaxed text-ink/80">
            Pick your favourites, place your order, and we'll have it hot and ready for you.
          </p>
          <a
            href="#menu"
            className="mt-8 inline-flex min-h-12 items-center gap-2 rounded-xl bg-primary px-6 font-semibold text-white transition-colors hover:bg-primary-hover"
          >
            Order now
            <ArrowDownIcon className="h-5 w-5" />
          </a>
        </div>

        <div className="relative aspect-[4/3] overflow-hidden rounded-3xl bg-line/40">
          {featured && (
            <>
              <img
                src={imageUrl(featured)}
                alt={featured.name}
                className="h-full w-full object-cover"
              />
              <div className="absolute bottom-4 left-4 rounded-xl bg-cream/95 px-4 py-2 shadow">
                <p className="font-display text-lg font-bold">{featured.name}</p>
                <p className="font-semibold tabular-nums text-accent">{formatPrice(featured.price)}</p>
              </div>
            </>
          )}
        </div>
      </section>

      {/* Menu */}
      <section id="menu" className="mx-auto max-w-7xl scroll-mt-20 px-4 pb-20 sm:px-6 lg:px-8">
        <h2 className="font-display text-3xl font-bold sm:text-4xl">Our menu</h2>

        {status === "ready" && items.length > 0 && (
          <div className="mt-6 flex flex-wrap gap-2" role="group" aria-label="Filter by category">
            {categories.map((c) => (
              <button
                key={c}
                type="button"
                onClick={() => setCategory(c)}
                aria-pressed={category === c}
                className={`min-h-11 rounded-full border px-5 text-sm font-semibold transition-colors ${
                  category === c
                    ? "border-primary bg-primary text-white"
                    : "border-line bg-white hover:border-primary hover:text-primary"
                }`}
              >
                {c}
              </button>
            ))}
          </div>
        )}

        <div className="mt-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {status === "loading" &&
            Array.from({ length: 6 }, (_, i) => (
              <div key={i} className="animate-pulse overflow-hidden rounded-2xl border border-line bg-white">
                <div className="aspect-[4/3] bg-line/40" />
                <div className="space-y-3 p-5">
                  <div className="h-5 w-2/3 rounded bg-line/60" />
                  <div className="h-4 w-full rounded bg-line/40" />
                </div>
              </div>
            ))}

          {status === "ready" &&
            visible.map((item) => (
              <article
                key={item.id}
                className="group flex flex-col overflow-hidden rounded-2xl border border-line bg-white transition-shadow hover:shadow-lg"
              >
                <div className="relative aspect-[4/3] overflow-hidden bg-line/40">
                  <img
                    src={imageUrl(item)}
                    alt={item.name}
                    loading="lazy"
                    className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
                  />
                  {item.specialTag && (
                    <span className="absolute left-3 top-3 rounded-full bg-primary px-3 py-1 text-xs font-semibold text-white">
                      {item.specialTag}
                    </span>
                  )}
                </div>
                <div className="flex flex-1 flex-col p-5">
                  <div className="flex items-start justify-between gap-4">
                    <h3 className="font-display text-xl font-bold">{item.name}</h3>
                    <p className="font-bold tabular-nums text-accent">{formatPrice(item.price)}</p>
                  </div>
                  <p className="mt-1 text-xs font-semibold uppercase tracking-wide text-ink/60">
                    {item.category}
                  </p>
                  {item.description && (
                    <p className="mt-3 line-clamp-2 text-sm leading-relaxed text-ink/80">
                      {item.description}
                    </p>
                  )}
                  <div className="mt-auto flex justify-end pt-4">
                    {inCart.has(item.id) ? (
                      <QuantityStepper
                        quantity={inCart.get(item.id)!}
                        onChange={(q) => setQuantity(item, q)}
                        label={item.name}
                      />
                    ) : (
                      <button
                        type="button"
                        onClick={() => setQuantity(item, 1)}
                        className="inline-flex min-h-11 items-center gap-1.5 rounded-full bg-primary px-5 text-sm font-semibold text-white transition-colors hover:bg-primary-hover"
                      >
                        <PlusIcon className="h-4 w-4" />
                        Add<span className="sr-only"> {item.name} to cart</span>
                      </button>
                    )}
                  </div>
                </div>
              </article>
            ))}
        </div>

        {status === "ready" && items.length === 0 && (
          <p className="mt-8 text-ink/70">The menu is empty right now. Check back soon.</p>
        )}

        {status === "error" && (
          <div role="alert" className="mt-8 flex flex-col items-start gap-4 rounded-2xl border border-line bg-white p-6">
            <p className="flex items-center gap-2 font-semibold">
              <ExclamationTriangleIcon className="h-5 w-5 text-primary" />
              Couldn't load the menu. Is the API running?
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
      </section>
    </>
  );
}

export default Home;
