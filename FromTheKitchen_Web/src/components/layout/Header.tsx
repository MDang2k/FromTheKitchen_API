import { useEffect, useRef, useState } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import {
  ShoppingCartIcon,
  UserCircleIcon,
  ChevronDownIcon,
  ClipboardDocumentListIcon,
  Cog6ToothIcon,
  ArrowRightStartOnRectangleIcon,
  ShoppingBagIcon,
} from "@heroicons/react/24/outline";
import { ROUTES } from "../../utility/constants";
import { getUser, logout } from "../../utility/helperFunc";
import { useCart } from "../../store/cart";

const menuItemClass =
  "flex min-h-11 items-center gap-3 px-4 text-sm text-ink transition-colors hover:bg-primary/5 hover:text-primary";

function Header() {
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  // ponytail: re-read on every render; Header re-renders on navigation, and login/logout always navigate.
  const user = getUser();
  const isAdmin = user?.role === "Admin";
  const cartCount = useCart().reduce((n, l) => n + l.quantity, 0);

  // Close the user dropdown when clicking outside of it.
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setMenuOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  function handleLogout() {
    logout();
    setMenuOpen(false);
    navigate(ROUTES.LOGIN);
  }

  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    `flex min-h-11 items-center text-sm font-semibold transition-colors ${
      isActive ? "text-primary" : "text-ink hover:text-primary"
    }`;

  return (
    <header className="sticky top-0 z-50 border-b border-line bg-cream/90 backdrop-blur">
      <nav className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
        {/* Logo */}
        <NavLink to={ROUTES.HOME} className="font-display text-lg font-bold sm:text-2xl">
          <span className="text-ink">From</span>
          <span className="text-primary">TheKitchen</span>
        </NavLink>

        {/* My Orders */}
        <div className="hidden md:flex">
          <NavLink to={ROUTES.MY_ORDERS} className={navLinkClass}>
            My Orders
          </NavLink>
        </div>

        {/* Cart + User / Auth */}
        <div className="flex items-center gap-1 sm:gap-4">
          <NavLink
            to={ROUTES.CART}
            className="relative rounded-full p-2.5 text-ink transition-colors hover:bg-primary/10 hover:text-primary"
            aria-label={cartCount ? `Cart, ${cartCount} items` : "Cart"}
          >
            <ShoppingCartIcon className="h-6 w-6" />
            {cartCount > 0 && (
              <span className="absolute right-0.5 top-0.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1 text-xs font-bold tabular-nums text-white">
                {cartCount > 99 ? "99+" : cartCount}
              </span>
            )}
          </NavLink>

          {user ? (
            /* User dropdown */
            <div className="relative" ref={menuRef}>
              <button
                type="button"
                onClick={() => setMenuOpen((open) => !open)}
                className="flex min-h-11 items-center gap-1 rounded-full px-2 text-ink transition-colors hover:bg-primary/10 hover:text-primary"
                aria-haspopup="true"
                aria-expanded={menuOpen}
                aria-label="User menu"
              >
                <UserCircleIcon className="h-7 w-7" />
                <ChevronDownIcon
                  className={`h-4 w-4 transition-transform duration-200 ${menuOpen ? "rotate-180" : ""}`}
                />
              </button>

              {menuOpen && (
                <div
                  role="menu"
                  className="absolute right-0 mt-2 w-56 overflow-hidden rounded-xl border border-line bg-white py-1 shadow-lg"
                >
                  <div className="px-4 py-2" title={user.email}>
                    <p className="truncate text-sm font-semibold text-ink">{user.name}</p>
                    <p className="truncate text-xs text-ink/60">{user.email}</p>
                  </div>
                  <div className="my-1 border-t border-line" />
                  {/* The top-bar link is hidden below md, so phones get it here. */}
                  <NavLink
                    to={ROUTES.MY_ORDERS}
                    role="menuitem"
                    onClick={() => setMenuOpen(false)}
                    className={`${menuItemClass} md:hidden`}
                  >
                    <ShoppingBagIcon className="h-5 w-5" />
                    My Orders
                  </NavLink>
                  {isAdmin && (
                    <>
                      <NavLink
                        to={ROUTES.ORDER_MANAGEMENT}
                        role="menuitem"
                        onClick={() => setMenuOpen(false)}
                        className={menuItemClass}
                      >
                        <ClipboardDocumentListIcon className="h-5 w-5" />
                        Order Management
                      </NavLink>
                      <NavLink
                        to={ROUTES.MENU_MANAGEMENT}
                        role="menuitem"
                        onClick={() => setMenuOpen(false)}
                        className={menuItemClass}
                      >
                        <Cog6ToothIcon className="h-5 w-5" />
                        Menu Management
                      </NavLink>
                      <div className="my-1 border-t border-line" />
                    </>
                  )}
                  <button
                    type="button"
                    role="menuitem"
                    onClick={handleLogout}
                    className="flex min-h-11 w-full items-center gap-3 px-4 text-sm text-primary transition-colors hover:bg-primary/5"
                  >
                    <ArrowRightStartOnRectangleIcon className="h-5 w-5" />
                    Logout
                  </button>
                </div>
              )}
            </div>
          ) : (
            /* Auth buttons */
            <div className="flex items-center gap-2">
              <NavLink
                to={ROUTES.LOGIN}
                className="flex min-h-11 items-center rounded-lg px-3 text-sm font-semibold text-ink transition-colors hover:bg-primary/10 sm:px-4"
              >
                Login
              </NavLink>
              <NavLink
                to={ROUTES.REGISTER}
                className="flex min-h-11 items-center rounded-lg bg-primary px-3 text-sm font-semibold text-white transition-colors hover:bg-primary-hover sm:px-4"
              >
                Register
              </NavLink>
            </div>
          )}
        </div>
      </nav>
    </header>
  );
}

export default Header;
