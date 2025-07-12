import { config } from "dotenv";
import { defineConfig } from "orval";

config({ path: ".env" });

export default defineConfig({
  // key will be the namespace in the generated files
  api: {
    input: {
      target: process.env.NEXT_PUBLIC_API_URL + "swagger/v1/swagger.json",
    },
    output: {
      mode: "tags-split",
      target: "lib/api/endpoints",
      schemas: "lib/api/models",
      client: "react-query",
      httpClient: "fetch",
      override: {
        mutator: {
          path: "lib/api/fetcher.ts",
          name: "fetcher",
        },
        fetch: {
          includeHttpResponseReturnType: false,
        },
      },
    },
  },
  zod: {
    input: {
      target: process.env.NEXT_PUBLIC_API_URL + "swagger/v1/swagger.json",
    },
    output: {
      mode: "tags-split",
      client: "zod",
      target: "lib/api/endpoints",
      fileExtension: ".zod.ts",
    },
  },
});
