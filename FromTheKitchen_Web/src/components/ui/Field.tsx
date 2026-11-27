import { useState, type InputHTMLAttributes } from "react";
import { EyeIcon, EyeSlashIcon } from "@heroicons/react/24/outline";

interface FieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  name: string;
  hint?: string;
}

function Field({ label, name, hint, type = "text", ...props }: FieldProps) {
  const [show, setShow] = useState(false);
  const isPassword = type === "password";

  return (
    <div>
      <label htmlFor={name} className="mb-1.5 block text-sm font-semibold">
        {label}
      </label>
      <div className="relative">
        <input
          id={name}
          name={name}
          type={isPassword && show ? "text" : type}
          aria-describedby={hint ? `${name}-hint` : undefined}
          className={`min-h-12 w-full rounded-xl border border-line bg-white px-4 text-base text-ink placeholder:text-ink/40 focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20 ${
            isPassword ? "pr-12" : ""
          }`}
          {...props}
        />
        {isPassword && (
          <button
            type="button"
            onClick={() => setShow((s) => !s)}
            aria-label={show ? "Hide password" : "Show password"}
            className="absolute inset-y-0 right-0 flex w-12 items-center justify-center text-ink/60 hover:text-primary"
          >
            {show ? <EyeSlashIcon className="h-5 w-5" /> : <EyeIcon className="h-5 w-5" />}
          </button>
        )}
      </div>
      {hint && (
        <p id={`${name}-hint`} className="mt-1.5 text-sm text-ink/60">
          {hint}
        </p>
      )}
    </div>
  );
}

export default Field;
