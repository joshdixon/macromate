import * as React from 'react';
import { Text as RNText, TextProps as RNTextProps } from 'react-native';
import { cn } from '@/lib/utils';

export type ThemedTextType =
  | 'title'
  | 'subtitle'
  | 'default'
  | 'defaultSemiBold'
  | 'link';

interface Props extends RNTextProps {
  type?: ThemedTextType;
  className?: string;
}

const typeClasses: Record<ThemedTextType, string> = {
  title: 'text-2xl font-bold text-foreground',
  subtitle: 'text-lg text-gray-500 dark:text-gray-400',
  default: 'text-base text-foreground',
  defaultSemiBold: 'text-base font-semibold text-foreground',
  link: 'text-primary underline',
};

export function ThemedText({ type = 'default', className, ...rest }: Props) {
  return <RNText {...rest} className={cn(typeClasses[type], className)} />;
}
