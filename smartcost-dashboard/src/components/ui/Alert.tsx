import React from 'react';

interface AlertProps {
  children: React.ReactNode;
  variant?: 'default' | 'destructive' | 'warning' | 'success';
  className?: string;
}

interface AlertDescriptionProps {
  children: React.ReactNode;
  className?: string;
}

const alertVariants = {
  default: 'bg-blue-50 border-blue-200 text-blue-800',
  destructive: 'bg-red-50 border-red-200 text-red-800',
  warning: 'bg-yellow-50 border-yellow-200 text-yellow-800',
  success: 'bg-green-50 border-green-200 text-green-800',
};

export const Alert: React.FC<AlertProps> = ({ children, variant = 'default', className = '' }) => {
  return (
    <div className={`p-4 rounded-md border ${alertVariants[variant]} ${className}`}>
      {children}
    </div>
  );
};

export const AlertDescription: React.FC<AlertDescriptionProps> = ({ children, className = '' }) => {
  return (
    <div className={`text-sm ${className}`}>
      {children}
    </div>
  );
};