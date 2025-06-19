import React from 'react';
import { IconButton, IconButtonProps, Tooltip } from '@mui/material';

interface SafeIconButtonProps extends Omit<IconButtonProps, 'onClick'> {
    onClick?: () => void;
    tooltip?: string;
    children: React.ReactNode;
}

const SafeIconButton: React.FC<SafeIconButtonProps> = ({
    onClick,
    tooltip,
    children,
    disabled = false,
    ...props
}) => {
    const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
        event.preventDefault();
        event.stopPropagation();

        if (!disabled && typeof onClick === 'function') {
            try {
                onClick();
            } catch (error) {
                console.error('IconButton onClick error:', error);
            }
        }
    };

    const buttonElement = (
        <IconButton
            {...props}
            onClick={handleClick}
            disabled={disabled}
            sx={{
                transition: 'all 0.2s ease',
                '&:hover': {
                    transform: 'scale(1.1)',
                    bgcolor: 'action.hover',
                },
                '&:active': {
                    transform: 'scale(0.95)',
                },
                ...props.sx,
            }}
        >
            {children}
        </IconButton>
    );

    if (tooltip) {
        return (
            <Tooltip title={tooltip} arrow>
                {buttonElement}
            </Tooltip>
        );
    }

    return buttonElement;
};

export default SafeIconButton; 