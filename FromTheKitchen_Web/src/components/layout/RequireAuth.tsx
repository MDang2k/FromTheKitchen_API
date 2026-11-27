import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { ROUTES } from "../../utility/constants";
import { getUser } from "../../utility/helperFunc";

// Sends logged-out visitors to Login (and back afterwards); shows non-admins a message on admin pages.
// This only hides pages. The API checks the token and role on every request.
function RequireAuth({ admin = false, children }: { admin?: boolean; children: ReactNode }) {
  const { pathname } = useLocation();
  const user = getUser();

  if (!user) return <Navigate to={ROUTES.LOGIN} state={{ from: pathname }} replace />;
  if (admin && user.role !== "Admin") {
    return (
      <div className="mx-auto max-w-md px-4 py-20 text-center">
        <h1 className="font-display text-3xl font-bold">Admins only</h1>
        <p className="mt-2 text-ink/70">You don't have access to this page.</p>
      </div>
    );
  }
  return children;
}

export default RequireAuth;
