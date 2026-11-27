import { useState, type FormEvent } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { CheckCircleIcon, ExclamationCircleIcon } from "@heroicons/react/24/outline";
import Field from "../../components/ui/Field";
import { ROUTES } from "../../utility/constants";
import { apiFetch, saveToken } from "../../utility/helperFunc";

interface LoginState {
  registered?: boolean;
  email?: string;
  from?: string;
}

function Login() {
  const navigate = useNavigate();
  const state = (useLocation().state ?? {}) as LoginState;
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    setError("");
    setLoading(true);
    try {
      const { token } = await apiFetch<{ token: string }>("/api/Auth/login", "POST", {
        email: form.get("email"),
        password: form.get("password"),
      });
      saveToken(token);
      navigate(state.from ?? ROUTES.HOME, { replace: true });
    } catch (err) {
      setError((err as Error).message);
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-md px-4 py-12 sm:py-20">
      <h1 className="font-display text-4xl font-bold">Welcome back</h1>
      <p className="mt-2 text-ink/70">Log in to order and track your meals.</p>

      {state.registered && !error && (
        <p role="status" className="mt-6 flex items-center gap-2 rounded-xl bg-green-50 p-4 text-sm font-semibold text-green-800">
          <CheckCircleIcon className="h-5 w-5 shrink-0" />
          Account created. Log in to continue.
        </p>
      )}

      {error && (
        <p role="alert" className="mt-6 flex items-start gap-2 whitespace-pre-line rounded-xl bg-red-50 p-4 text-sm font-semibold text-primary-hover">
          <ExclamationCircleIcon className="h-5 w-5 shrink-0" />
          {error}
        </p>
      )}

      <form onSubmit={handleSubmit} className="mt-8 space-y-5">
        <Field label="Email" name="email" type="email" autoComplete="email" defaultValue={state.email} required autoFocus={!state.email} />
        <Field label="Password" name="password" type="password" autoComplete="current-password" required autoFocus={!!state.email} />
        <button
          type="submit"
          disabled={loading}
          className="min-h-12 w-full rounded-xl bg-primary font-semibold text-white transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
        >
          {loading ? "Logging in…" : "Log in"}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-ink/70">
        New here?{" "}
        <Link to={ROUTES.REGISTER} className="font-semibold text-primary hover:underline">
          Create an account
        </Link>
      </p>
    </div>
  );
}

export default Login;
