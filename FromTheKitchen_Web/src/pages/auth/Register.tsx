import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ExclamationCircleIcon } from "@heroicons/react/24/outline";
import Field from "../../components/ui/Field";
import { ROUTES } from "../../utility/constants";
import { apiFetch } from "../../utility/helperFunc";

function Register() {
  const navigate = useNavigate();
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    const email = form.get("email") as string;
    if (form.get("password") !== form.get("confirmPassword")) {
      setError("Passwords don't match.");
      return;
    }
    setError("");
    setLoading(true);
    try {
      // Role left empty: the API registers new accounts as Customer.
      await apiFetch("/api/Auth/register", "POST", {
        name: form.get("name"),
        email,
        password: form.get("password"),
      });
      navigate(ROUTES.LOGIN, { state: { registered: true, email } });
    } catch (err) {
      setError((err as Error).message);
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-md px-4 py-12 sm:py-20">
      <h1 className="font-display text-4xl font-bold">Create an account</h1>
      <p className="mt-2 text-ink/70">Order faster and keep track of your meals.</p>

      {error && (
        <p role="alert" className="mt-6 flex items-start gap-2 whitespace-pre-line rounded-xl bg-red-50 p-4 text-sm font-semibold text-primary-hover">
          <ExclamationCircleIcon className="h-5 w-5 shrink-0" />
          {error}
        </p>
      )}

      <form onSubmit={handleSubmit} className="mt-8 space-y-5">
        <Field label="Name" name="name" autoComplete="name" required autoFocus />
        <Field label="Email" name="email" type="email" autoComplete="email" required />
        <Field
          label="Password"
          name="password"
          type="password"
          autoComplete="new-password"
          minLength={6}
          required
          hint="At least 6 characters, with an uppercase letter, a lowercase letter, a number and a symbol."
        />
        <Field label="Confirm password" name="confirmPassword" type="password" autoComplete="new-password" required />
        <button
          type="submit"
          disabled={loading}
          className="min-h-12 w-full rounded-xl bg-primary font-semibold text-white transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
        >
          {loading ? "Creating account…" : "Create account"}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-ink/70">
        Already have an account?{" "}
        <Link to={ROUTES.LOGIN} className="font-semibold text-primary hover:underline">
          Log in
        </Link>
      </p>
    </div>
  );
}

export default Register;
