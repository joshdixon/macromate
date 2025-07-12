import * as React from 'react';
import { View, ViewProps } from 'react-native';
import { cn } from '@/lib/utils';

interface Props extends ViewProps {
  className?: string;
}

export function ThemedView({ className, style, ...rest }: Props) {
  return <View {...rest} style={style} className={cn(className)} />;
}
