import { create } from "zustand";
import { persist, createJSONStorage, combine } from "zustand/middleware";

const useAuthStore = create(
  persist(
    combine(
      {
        accessToken: null as string | null,
        isHydrated: false,
      },
      (set) => ({
        setAccessToken: (accessToken: string | null) => set({ accessToken }),
        logOut: () => set({ accessToken: null }),
        setHydrated: () => set({ isHydrated: true }),
      }),
    ),
    {
      name: "auth-storage",
      storage: createJSONStorage(() => localStorage),
      onRehydrateStorage: () => (state) => {
        state?.setHydrated();
      },
    },
  ),
);

type AuthState = ReturnType<typeof useAuthStore>;

export default useAuthStore;
