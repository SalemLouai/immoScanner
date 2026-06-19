import React from 'react';

interface ScoreBadgeProps {
  score: number;
  size?: 'sm' | 'md' | 'lg';
}

/** Color-coded opportunity score badge (0–100). */
export const ScoreBadge: React.FC<ScoreBadgeProps> = ({ score, size = 'md' }) => {
  const color =
    score >= 70 ? '#16a34a'   // green-600
    : score >= 50 ? '#ca8a04' // yellow-600
    : '#dc2626';              // red-600

  const padding =
    size === 'sm' ? '2px 6px'
    : size === 'lg' ? '6px 14px'
    : '4px 10px';

  const fontSize =
    size === 'sm' ? '0.75rem'
    : size === 'lg' ? '1.125rem'
    : '0.875rem';

  return (
    <span
      style={{
        backgroundColor: color,
        color: '#fff',
        borderRadius: '9999px',
        padding,
        fontSize,
        fontWeight: 700,
        display: 'inline-block',
        minWidth: '2.5rem',
        textAlign: 'center',
      }}
    >
      {score}
    </span>
  );
};
