import { useEffect, useRef, useState, type FormEvent } from "react";
import { ExclamationCircleIcon, ExclamationTriangleIcon, PencilSquareIcon, PhotoIcon, PlusIcon, TrashIcon, XMarkIcon } from "@heroicons/react/24/outline";
import Field from "../../components/ui/Field";
import { API_BASE_URL, type MenuItem } from "../../utility/constants";
import { apiFetch, formatPrice, imageUrl } from "../../utility/helperFunc";

// Must match the API's checks in MenuItemController.
const IMAGE_TYPES = ".jpg,.jpeg,.png,.webp,.gif";
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;

function MenuItemManagement() {
  const [items, setItems] = useState<MenuItem[]>([]);
  const [status, setStatus] = useState<"loading" | "error" | "ready">("loading");
  const [loadError, setLoadError] = useState("");
  const [reload, setReload] = useState(0);
  const [deleting, setDeleting] = useState<number | null>(null);

  // Form dialog: editing === null means "add new".
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [editing, setEditing] = useState<MenuItem | null>(null);
  const [formKey, setFormKey] = useState(0);
  const [preview, setPreview] = useState("");
  const [formError, setFormError] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let active = true;
    fetch(`${API_BASE_URL}/api/MenuItem`)
      .then((res) => (res.ok ? res.json() : Promise.reject()))
      .then((data) => {
        if (!active) return;
        setItems(data.result ?? []);
        setStatus("ready");
      })
      .catch(() => {
        if (!active) return;
        setLoadError("Couldn't load the menu. Is the API running?");
        setStatus("error");
      });
    return () => {
      active = false;
    };
  }, [reload]);

  // Free the object URL made for a picked file.
  useEffect(() => () => (preview.startsWith("blob:") ? URL.revokeObjectURL(preview) : undefined), [preview]);

  const categories = [...new Set(items.map((i) => i.category).filter(Boolean))];
  const tags = [...new Set(items.map((i) => i.specialTag).filter(Boolean))] as string[];

  function openForm(item: MenuItem | null) {
    setEditing(item);
    setFormKey((k) => k + 1);
    setPreview(item ? imageUrl(item) : "");
    setFormError("");
    dialogRef.current?.showModal();
  }

  function pickImage(file: File | undefined) {
    setFormError("");
    if (!file) return;
    if (file.size > MAX_IMAGE_BYTES) {
      setFormError("The image must be 5 MB or smaller.");
      return;
    }
    setPreview(URL.createObjectURL(file));
  }

  async function save(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    const file = form.get("File") as File | null;
    if (!file || file.size === 0) form.delete("File"); // no new image: the API keeps the current one
    else if (file.size > MAX_IMAGE_BYTES) return setFormError("The image must be 5 MB or smaller.");

    setSaving(true);
    setFormError("");
    try {
      if (editing) {
        form.set("Id", String(editing.id));
        await apiFetch(`/api/MenuItem?id=${editing.id}`, "PUT", form);
      } else {
        await apiFetch("/api/MenuItem", "POST", form);
      }
      dialogRef.current?.close();
      setReload((n) => n + 1);
    } catch (err) {
      setFormError((err as Error).message);
    } finally {
      setSaving(false);
    }
  }

  async function remove(item: MenuItem) {
    if (!window.confirm(`Delete "${item.name}" from the menu? This can't be undone.`)) return;
    setDeleting(item.id);
    try {
      await apiFetch(`/api/MenuItem?id=${item.id}`, "DELETE");
      setItems((list) => list.filter((i) => i.id !== item.id));
    } catch (err) {
      window.alert((err as Error).message);
    } finally {
      setDeleting(null);
    }
  }

  return (
    <div className="mx-auto max-w-5xl px-4 py-12 sm:px-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="font-display text-4xl font-bold">Menu management</h1>
          {status === "ready" && (
            <p className="mt-1 text-sm text-ink/60">
              {items.length} {items.length === 1 ? "item" : "items"} on the menu
            </p>
          )}
        </div>
        <button
          type="button"
          onClick={() => openForm(null)}
          className="inline-flex min-h-11 items-center gap-2 rounded-xl bg-primary px-5 font-semibold text-white transition-colors hover:bg-primary-hover"
        >
          <PlusIcon className="h-5 w-5" />
          Add item
        </button>
      </div>

      {status === "loading" && (
        <div className="mt-8 space-y-3" aria-busy="true">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-24 animate-pulse rounded-2xl border border-line bg-white" />
          ))}
        </div>
      )}

      {status === "error" && (
        <div role="alert" className="mt-8 flex flex-col items-start gap-4 rounded-2xl border border-line bg-white p-6">
          <p className="flex items-center gap-2 font-semibold">
            <ExclamationTriangleIcon className="h-5 w-5 text-primary" />
            {loadError}
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

      {status === "ready" && items.length === 0 && (
        <p className="mt-10 text-center text-ink/70">The menu is empty. Add your first dish.</p>
      )}

      {status === "ready" && items.length > 0 && (
        <ul className="mt-8 divide-y divide-line rounded-2xl border border-line bg-white">
          {items.map((item) => (
            <li key={item.id} className="flex flex-wrap items-center gap-4 p-4 sm:flex-nowrap">
              <img src={imageUrl(item)} alt="" className="h-16 w-16 shrink-0 rounded-xl bg-line/40 object-cover" />
              <div className="min-w-0 flex-1">
                <p className="font-display text-lg font-bold">{item.name}</p>
                <p className="flex flex-wrap items-center gap-2 text-sm text-ink/70">
                  {item.category || "No category"}
                  {item.specialTag && (
                    <span className="rounded-full bg-primary px-2 py-0.5 text-xs font-semibold text-white">{item.specialTag}</span>
                  )}
                </p>
              </div>
              <p className="font-bold tabular-nums text-accent sm:w-20 sm:text-right">{formatPrice(item.price)}</p>
              <div className="ml-auto flex gap-1">
                <button
                  type="button"
                  onClick={() => openForm(item)}
                  aria-label={`Edit ${item.name}`}
                  className="flex h-11 w-11 items-center justify-center rounded-full text-ink/70 transition-colors hover:bg-primary/10 hover:text-primary"
                >
                  <PencilSquareIcon className="h-5 w-5" />
                </button>
                <button
                  type="button"
                  onClick={() => remove(item)}
                  disabled={deleting === item.id}
                  aria-label={`Delete ${item.name}`}
                  className="flex h-11 w-11 items-center justify-center rounded-full text-ink/70 transition-colors hover:bg-primary/10 hover:text-primary disabled:opacity-40"
                >
                  <TrashIcon className="h-5 w-5" />
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}

      <dialog
        ref={dialogRef}
        aria-labelledby="menu-form-title"
        className="m-auto w-[calc(100%-2rem)] max-w-lg rounded-2xl bg-cream p-0 text-ink shadow-xl backdrop:bg-black/50"
      >
        {/* A new key on every open gives fresh inputs (uncontrolled, so defaultValue only applies on mount). */}
        <form key={formKey} onSubmit={save} className="max-h-[85dvh] space-y-5 overflow-y-auto p-6">
          <div className="flex items-start justify-between gap-4">
            <h2 id="menu-form-title" className="font-display text-2xl font-bold">
              {editing ? `Edit ${editing.name}` : "Add a menu item"}
            </h2>
            <button
              type="button"
              onClick={() => dialogRef.current?.close()}
              aria-label="Close"
              className="-mr-2 -mt-2 flex h-11 w-11 shrink-0 items-center justify-center rounded-full hover:bg-primary/10"
            >
              <XMarkIcon className="h-6 w-6" />
            </button>
          </div>

          {formError && (
            <p role="alert" className="flex items-start gap-2 whitespace-pre-line rounded-xl bg-red-50 p-4 text-sm font-semibold text-primary-hover">
              <ExclamationCircleIcon className="h-5 w-5 shrink-0" />
              {formError}
            </p>
          )}

          <Field label="Name" name="Name" defaultValue={editing?.name} required />
          <div>
            <label htmlFor="Description" className="mb-1.5 block text-sm font-semibold">
              Description
            </label>
            <textarea
              id="Description"
              name="Description"
              rows={3}
              defaultValue={editing?.description ?? ""}
              className="w-full rounded-xl border border-line bg-white px-4 py-3 text-base focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
            />
          </div>
          <div className="grid gap-5 sm:grid-cols-2">
            <Field label="Category" name="Category" list="menu-categories" defaultValue={editing?.category} required />
            <Field label="Special tag" name="SpecialTag" list="menu-tags" defaultValue={editing?.specialTag ?? ""} hint="Optional, e.g. Best Seller" />
          </div>
          <datalist id="menu-categories">
            {categories.map((c) => <option key={c} value={c} />)}
          </datalist>
          <datalist id="menu-tags">
            {tags.map((t) => <option key={t} value={t} />)}
          </datalist>
          <Field label="Price (USD)" name="Price" type="number" inputMode="decimal" min={1} max={1000} step={0.01} defaultValue={editing?.price} required />

          <div>
            <label htmlFor="File" className="mb-1.5 block text-sm font-semibold">
              Image
            </label>
            <div className="flex items-center gap-4">
              <div className="flex h-20 w-20 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-line/40">
                {preview ? <img src={preview} alt="" className="h-full w-full object-cover" /> : <PhotoIcon className="h-8 w-8 text-ink/40" />}
              </div>
              <input
                id="File"
                name="File"
                type="file"
                accept={IMAGE_TYPES}
                required={!editing}
                onChange={(e) => pickImage(e.target.files?.[0])}
                aria-describedby="File-hint"
                className="min-w-0 flex-1 text-sm file:mr-3 file:min-h-11 file:cursor-pointer file:rounded-xl file:border-0 file:bg-primary/10 file:px-4 file:font-semibold file:text-primary hover:file:bg-primary/20"
              />
            </div>
            <p id="File-hint" className="mt-1.5 text-sm text-ink/60">
              JPG, PNG, WebP or GIF, up to 5 MB.{editing && " Leave empty to keep the current image."}
            </p>
          </div>

          <div className="flex flex-wrap justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={() => dialogRef.current?.close()}
              className="min-h-11 rounded-xl border border-line bg-white px-5 font-semibold transition-colors hover:border-primary hover:text-primary"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={saving}
              className="min-h-11 rounded-xl bg-primary px-5 font-semibold text-white transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
            >
              {saving ? "Saving…" : editing ? "Save changes" : "Add item"}
            </button>
          </div>
        </form>
      </dialog>
    </div>
  );
}

export default MenuItemManagement;
