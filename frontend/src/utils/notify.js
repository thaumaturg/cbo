import { useToast } from "primevue/usetoast";

export function useNotify() {
  const toast = useToast();

  return {
    success: (summary, detail) => toast.add({ severity: "success", summary, detail, life: 3000 }),
    error: (summary, detail) => toast.add({ severity: "error", summary, detail, life: 5000 }),
    warn: (summary, detail) => toast.add({ severity: "warn", summary, detail, life: 3000 }),
  };
}

