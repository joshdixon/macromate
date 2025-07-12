import { View } from "react-native";
import { Text } from "@/components/ui/text";
import { Button } from "@/components/ui/button";
import * as AuthSession from "expo-auth-session";
import * as WebBrowser from "expo-web-browser";
import {
  useWorkOsRedirect,
  useAuthenticateWorkOsUser,
} from "@/lib/api/endpoints/macromate-webapi/macromate-webapi";
import useAuthStore from "@/hooks/useAuthStore";
import { useRouter } from "expo-router";
import * as React from "react";

export default function LoginScreen() {
  const redirectMutation = useWorkOsRedirect();
  const authMutation = useAuthenticateWorkOsUser();
  const setAccessToken = useAuthStore((state) => state.setAccessToken);
  const router = useRouter();

  const handleLogin = async () => {
    try {
      const redirect = AuthSession.makeRedirectUri();
      const redirectUrlResult = await redirectMutation.mutateAsync({
        data: {
          provider: "GoogleOAuth",
          returnPath: redirect,
        },
      });

      const result = await WebBrowser.openAuthSessionAsync(
        redirectUrlResult.redirectUrl,
        redirect,
      );

      const code = /code=([^&]+)/.exec(result.url)?.[1] ?? null;
      const state = /state=([^&]+)/.exec(result.url)?.[1] ?? null;

      if (!code || !state) {
        console.warn("Login cancelled or no code/state returned");
        return;
      }

      authMutation.mutate(
        {
          data: {
            code,
            state,
            redirectUri: `${redirect}`,
          },
        },
        {
          onSuccess: ({ accessToken }) => {
            setAccessToken(accessToken);
            router.replace("/(tabs)");
          },
        },
      );
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <View className="flex-1 items-center justify-center p-6 bg-white gap-y-6">
      <Text className="text-2xl font-bold text-black">Welcome to MacroMate</Text>
      <Button onPress={handleLogin} variant="default" size="lg">
        <Text className="text-black">Sign in with Google</Text>
      </Button>
    </View>
  );
}
