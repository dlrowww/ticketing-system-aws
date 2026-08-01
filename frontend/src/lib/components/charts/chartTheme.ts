function cssVar(name: string): string {
	if (typeof window === 'undefined') {
		return '';
	}
	return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
}

export function getChartPalette() {
	// Prefer IronPack theme variables when present, otherwise fall back to Bootstrap.
	const primary = cssVar('--ironpack-red') || cssVar('--bs-primary');
	const secondary = cssVar('--bs-secondary');
	const success = cssVar('--bs-success');
	const danger = cssVar('--bs-danger');
	const warning = cssVar('--bs-warning');
	const info = cssVar('--bs-info');
	const light = cssVar('--bs-light');
	const dark = cssVar('--bs-dark');
	const ticketTrendColor = cssVar('--ironpack-ticket-trend-color') || '#51cf66';

	// Pastel colors for category charts (softer, more pleasant)
	// const pastelSeries = [
	// 	'#a8d5e2',  // Soft blue
	// 	'#ffd4a3',  // Soft orange
	// 	'#c9b8d8',  // Soft purple
	// 	'#b4e7ce',  // Soft green
	// 	'#ffb6c1',  // Soft pink
	// 	'#f7dc6f',  // Soft yellow
	// 	'#d4a5a5'   // Soft brown
	// ];
	const pastelSeries = [
		'#74c0fc', // Soft blue
		'#ffd8a8', // Soft orange
		'#d0bfff', // Soft violet
		'#b2f2bb', // Soft mint
		'#fcc2d7', // Soft pink
		'#c5f6fa', // Soft cyan
		'#eebefa', // Soft purple
		'#ffe8cc', // Light peach
		'#a5d8ff', // Light blue
		'#d3f9d8', // Light mint
		'#bac8ff', // Soft indigo
		'#f1f3f5', // Light gray
		'#dee2e6', // Cool gray
		'#ced4da', // Steel gray
		'#e5dbff'  // Light lavender
	];

	return {
		primary,
		secondary,
		success,
		danger,
		warning,
		info,
		light,
		dark,
		ticketTrendColor,
		series: [primary, success, warning, danger, info, secondary, dark].filter(Boolean),
		pastelSeries
	};
}
