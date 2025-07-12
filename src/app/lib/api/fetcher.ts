import useAuthStore from "@/hooks/useAuthStore";

let refreshPromise: Promise<void> | null = null;

async function refreshTokensIfNeeded(): Promise<void> {
  const auth = {
    refreshToken: null,
    accessToken: null,
    setTokens: (accessToken: string, refreshToken: string) => {},
    logout: () => {},
  };
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    const res = await fetch(getUrl(`/auth/refresh`), {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: auth?.refreshToken }),
    });
    if (!res.ok) {
      refreshPromise = null;
      const err = await res.text();
      auth.logout();
      throw new Error(`Refresh failed ${res.status}: ${err}`);
    }
    const { accessToken, refreshToken } = (await res.json()) as {
      accessToken: string;
      refreshToken: string;
    };
    auth.setTokens(accessToken, refreshToken);
    refreshPromise = null;
  })();

  return refreshPromise;
}

const getUrl = (contextUrl: string): string => {
  let baseUrl = process.env.NEXT_PUBLIC_API_URL;
  // ensure only one slash between base and context
  if (baseUrl?.endsWith("/")) {
    baseUrl = baseUrl.slice(0, -1);
  }
  const requestUrl = new URL(`${baseUrl}${contextUrl}`);
  return requestUrl.toString();
};

export async function fetcher<TData>(
  url: string,
  options: RequestInit = {},
): Promise<TData> {
  async function doRequest(): Promise<Response> {
    const accessToken = useAuthStore.getState().accessToken;

    const headers = new Headers(options.headers);
    if (!headers.has("Content-Type")) {
      headers.set("Content-Type", "application/json");
    }
    if (accessToken) {
      headers.set("Authorization", `Bearer ${accessToken}`);
    }

    console.log(`Fetching ${url} with options:`, options);

    const fetchInit: RequestInit = {
      ...options,
      method: options.method,
      headers: headers,
      body: options.body != null ? options.body : undefined,
      credentials: "include",
      signal: options.signal,
    };

    const requestUrl = getUrl(url);

    return fetch(requestUrl, fetchInit);
  }

  // 1st attempt
  let response = await doRequest();

  const refreshToken = null;

  // if 401, refresh once (singleton) then retry
  if (response.status === 401 && refreshToken) {
    await refreshTokensIfNeeded();
    response = await doRequest();
  }

  if (!response.ok) {
    throw (await response.json()) as unknown;
  }

  if (!response.headers.get("Content-Type")?.includes("application/json")) {
    return (await response.text()) as unknown as TData;
  }

  return (await response.json()) as TData;
}
