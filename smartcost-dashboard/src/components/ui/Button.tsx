import React from 'react';

interface ButtonProps {
  children: React.ReactNode;
  variant?: 'default' | 'destructive' | 'outline' | 'secondary' | 'ghost' | 'link';
  size?: 'default' | 'sm' | 'lg';
  className?: string;
  disabled?: boolean;
  onClick?: () => void;
  type?: 'button' | 'submit' | 'reset';
}

const buttonVariants = {
  default: 'bg-blue-600 hover:bg-blue-700 text-white',
  destructive: 'bg-red-600 hover:bg-red-700 text-white',
  outline: 'border border-gray-300 hover:bg-gray-50 text-gray-700',
  secondary: 'bg-gray-100 hover:bg-gray-200 text-gray-900',
  ghost: 'hover:bg-gray-100 text-gray-700',
  link: 'text-blue-600 hover:text-blue-800 underline',
};

const buttonSizes = {
  default: 'px-4 py-2 text-sm',
  sm: 'px-3 py-1.5 text-xs',
  lg: 'px-6 py-3 text-base',
};

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'default',
  size = 'default',
  className = '',
  disabled = false,
  onClick,
  type = 'button',
}) => {
  return (
    <button
      type={type}
      className={`
        inline-flex items-center justify-center rounded-md font-medium transition-colors
        focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2
        disabled:opacity-50 disabled:cursor-not-allowed
        ${buttonVariants[variant]}
        ${buttonSizes[size]}
        ${className}
      `}
      disabled={disabled}
      onClick={onClick}
    >
      {children}
    </button>
  );
};