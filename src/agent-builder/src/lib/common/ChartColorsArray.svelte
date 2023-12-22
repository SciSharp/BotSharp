<script context="module">
	import { browser } from '$app/environment';

	export function getChartColorsArray(colors) {
		if (browser) {
			if (colors) {
				return colors.map(function (value) {
					var newValue = value.replace(' ', '');
					if (newValue.indexOf(',') === -1) {
						var color = getComputedStyle(document.documentElement).getPropertyValue(newValue);
						if (color) return color;
						else return newValue;
					} else {
						var val = value.split(',');
						if (val.length == 2) {
							var rgbaColor = getComputedStyle(document.documentElement).getPropertyValue(val[0]);
							rgbaColor = 'rgba(' + rgbaColor + ',' + val[1] + ')';
							return rgbaColor;
						} else {
							return newValue;
						}
					}
				});
			}
		}
	}
</script>
