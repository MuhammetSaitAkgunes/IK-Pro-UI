import { createContext, useCallback, useContext, useState, type ReactNode } from "react";

type ToastType = "success" | "error" | "warning" | "info";
type Toast = { id: number; message: string; type: ToastType };

const icons: Record<ToastType, string> = {
  success: "fa-circle-check", error: "fa-circle-xmark",
  warning: "fa-triangle-exclamation", info: "fa-circle-info",
};

const ToastContext = createContext<{ showToast: (message: string, type?: ToastType) => void } | null>(null);
let nextId = 1;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const showToast = useCallback((message: string, type: ToastType = "success") => {
    const id = nextId++;
    setToasts((current) => [...current, { id, message, type }]);
    setTimeout(() => setToasts((current) => current.filter((t) => t.id !== id)), 3200);
  }, []);

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div id="toast-region" className="toast-region" aria-live="polite">
        {toasts.map((toast) => (
          <div key={toast.id} className={`toast toast-${toast.type} visible`}>
            <i aria-hidden="true" className={`fa-solid ${icons[toast.type]}`} />
            <span>{toast.message}</span>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const value = useContext(ToastContext);
  if (!value) throw new Error("useToast, ToastProvider içinde kullanılmalı.");
  return value;
}
