export function isEmpty(obj: any) {
	return [Object, Array].includes((obj || {}).constructor) && !Object.entries(obj || {}).length;
}

export function isArray(object: any) {
	return Array.isArray(object);
}

export function parsePathSegment(path: string) {
	return isEmpty(path)
		? []
		: path.match(/"([^"]+)"|[^.[\]]+/g)?.map((s: string) => s.replace(/"/g, '')) || [];
}

export function getProperty(obj: any, keyList: string): any {
	if (!keyList) {
		return null;
	}
	if (obj === undefined || obj === null) {
		return null;
	}

	const keys = parsePathSegment(keyList);

	if (keys.length === 0) {
		return null;
	}

	const key = keys[0];

	if (keys.length === 1) {
		//last key in list
		if (!obj.hasOwnProperty(key)) {
			return null;
		}
		return obj[key];
	} else {
		if (!obj.hasOwnProperty(key)) {
			return null;
		}
		const keyObj: any = obj[key];
		if (isArray(keyObj)) {
			const index: number = Number(keys[1]) || 0;
			if (keys.length === 2) {
				return keyObj[index];
			} else {
				return getProperty(keyObj[index], keys.slice(2).join('.'));
			}
		}
		return getProperty(keyObj, keys.slice(1).join('.'));
	}
}

export function paramsToString(template: string, paramsObj: Record<string, any>): string {
	return template.replace(/{([^{}]*)}/g, function (a, b) {
		const r = paramsObj[b];
		return typeof r === 'string' || typeof r === 'number' ? `${r}` : a;
	});
}

export default {
	getProperty,
	isArray,
	isEmpty,
	paramsToString,
	parsePathSegment
};
