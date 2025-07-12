import { create } from "zustand";
import { persist, createJSONStorage, combine } from "zustand/middleware";

const useOAuthStore = create(
  persist(
    combine(
      {
        redirectUri: null as string | null,
        codeVerifier: null as string | null,
      },
      (set) => ({
        setRedirectUri: (redirectUri: string | null) => set({ redirectUri }),
        setCodeVerifier: (codeVerifier: string | null) => set({ codeVerifier }),
      }),
    ),
    {
      name: "oauth-storage", // name of the item in the storage
      storage: createJSONStorage(() => localStorage), // use local storage
    },
  ),
);

type OAuthState = ReturnType<typeof useOAuthStore>;

export default useOAuthStore;
