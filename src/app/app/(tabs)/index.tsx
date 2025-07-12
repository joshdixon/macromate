import { Image } from "expo-image";
import { Platform, StyleSheet } from "react-native";

import { HelloWave } from "@/components/HelloWave";
import ParallaxScrollView from "@/components/ParallaxScrollView";
import { ThemedText } from "@/components/ThemedText";
import { ThemedView } from "@/components/ThemedView";
import {Button} from "@/components/ui/button";
import * as AuthSession from 'expo-auth-session';
import * as WebBrowser from 'expo-web-browser';
import {useAuthenticateWorkOsUser, useWorkOsRedirect} from "@/lib/api/endpoints/macromate-webapi/macromate-webapi";
import useAuthStore from "@/hooks/useAuthStore";
import { useRouter } from "expo-router";
import {useEffect} from "react";

export default function HomeScreen() {
  const redirectMutation = useWorkOsRedirect();
  const authMutation = useAuthenticateWorkOsUser();
  const setAccessToken = useAuthStore((state) => state.setAccessToken);
  const accessToken = useAuthStore((state)=>state.accessToken);
  const router = useRouter();

  useEffect(()=>{
    if(!accessToken){
      router.replace("/login");
    }
  },[accessToken]);

  const handleLogin = async () => {
    console.log("Login");
    try {
      const redirect = AuthSession.makeRedirectUri().toString();
      const redirectUrlResult = await redirectMutation.mutateAsync({
        data: {
          provider: "GoogleOAuth",
          returnPath: redirect,
        },
      });

      // Call openAuthSessionAsync with the url and redirect from above, and save the returned object to a variable
      const result = await WebBrowser.openAuthSessionAsync(redirectUrlResult.redirectUrl, redirect);

      // Pull the code returned in the result stored as a param in the url field. In this case, we are using a regular expression pattern to pull it from the url.
      const codeRegex = /code=([^&]+)/;
      const matches = result.url.match(codeRegex);
      const code = matches ? matches[1] : null;
      
      const stateRegex = /state=([^&]+)/;
      const stateMatches = result.url.match(stateRegex);
      const state = stateMatches ? stateMatches[1] : null;
      
      console.log("code: ", code);
      authMutation.mutate(
        {
          data: {
            code,
            state,
            redirectUri: `${window.location.origin}/workos-callback`,
          },
        },
        {
          onSuccess: ({accessToken, refreshToken}) => {
            // TODO: stash tokens (cookie / localStorage / NextAuth signIn())
            console.log("JWT", accessToken, "refresh", refreshToken);
            setAccessToken(accessToken);
            //
            // router.replace("/app/environments/create");
          },
          onError: () => {
            // handled below via authMutation.isError
          },
        },
      );
    }
    catch (error) {
      console.log(error);
    }
  };
  return (
    <ParallaxScrollView
      headerBackgroundColor={{ light: "#A1CEDC", dark: "#1D3D47" }}
      headerImage={
        <Image
          source={require("@/assets/images/partial-react-logo.png")}
          style={styles.reactLogo}
        />
      }
    >
      <ThemedView style={styles.titleContainer}>
        <ThemedText type="title">Welcome!</ThemedText>
        <HelloWave />
      </ThemedView>
      <ThemedView style={styles.stepContainer}>
        <ThemedText type="subtitle">Step 1: Try it!</ThemedText>
        <ThemedText>
          Edit{" "}
          <ThemedText type="defaultSemiBold">app/(tabs)/index.tsx</ThemedText>{" "}
          to see changes. Press{" "}
          <ThemedText type="defaultSemiBold">
            {Platform.select({
              ios: "cmd + d",
              android: "cmd + m",
              web: "F12",
            })}
          </ThemedText>{" "}
          to open developer tools.
        </ThemedText>
        <Button onPress={handleLogin}>
          <ThemedText type="link">Learn more</ThemedText>
        </Button>
      </ThemedView>
      <ThemedView style={styles.stepContainer}>
        <ThemedText type="subtitle">Step 2: Explore</ThemedText>
        <ThemedText>
          {`Tap the Explore tab to learn more about what's included in this starter app.`}
        </ThemedText>
      </ThemedView>
      <ThemedView style={styles.stepContainer}>
        <ThemedText type="subtitle">Step 3: Get a fresh start</ThemedText>
        <ThemedText>
          {`When you're ready, run `}
          <ThemedText type="defaultSemiBold">
            npm run reset-project
          </ThemedText>{" "}
          to get a fresh <ThemedText type="defaultSemiBold">app</ThemedText>{" "}
          directory. This will move the current{" "}
          <ThemedText type="defaultSemiBold">app</ThemedText> to{" "}
          <ThemedText type="defaultSemiBold">app-example</ThemedText>.
        </ThemedText>
      </ThemedView>
    </ParallaxScrollView>
  );
}

const styles = StyleSheet.create({
  titleContainer: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
  },
  stepContainer: {
    gap: 8,
    marginBottom: 8,
  },
  reactLogo: {
    height: 178,
    width: 290,
    bottom: 0,
    left: 0,
    position: "absolute",
  },
});
