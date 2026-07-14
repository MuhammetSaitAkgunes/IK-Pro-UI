type ChartProps = { data?: unknown; options?: unknown };

export const Line = (_props: ChartProps) => <canvas data-testid="chart-line" />;
export const Doughnut = (_props: ChartProps) => <canvas data-testid="chart-doughnut" />;
export const Bar = (_props: ChartProps) => <canvas data-testid="chart-bar" />;
