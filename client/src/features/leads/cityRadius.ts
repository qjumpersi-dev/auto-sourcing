import {
  buildCountryLookup,
  findCityCoords,
  findCountryInfo,
  loadCities,
  loadCountriesIndex,
  normalizeName,
} from './geo/geoData'

const EARTH_RADIUS_MILES = 3958.8
const MAX_CITIES = 60

function distanceMiles(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const toRad = (value: number) => (value * Math.PI) / 180
  const dLat = toRad(lat2 - lat1)
  const dLon = toRad(lon2 - lon1)
  const a =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLon / 2) ** 2
  return 2 * EARTH_RADIUS_MILES * Math.asin(Math.sqrt(a))
}

export interface RadiusResult {
  names: string[]
  note: string
}

// Finds the towns/cities within `miles` of the chosen city, so they can be added to the search.
export async function findCitiesWithinRadius(
  cityName: string,
  countryName: string,
  stateName: string,
  miles: number,
): Promise<RadiusResult | null> {
  const name = (cityName ?? '').trim()
  if (!name) {
    return null
  }

  const cities = await loadCities()
  if (!cities) {
    return null
  }

  let countryA2: string | undefined
  const countryInput = (countryName ?? '').trim()
  if (countryInput) {
    const index = await loadCountriesIndex()
    if (index) {
      countryA2 = findCountryInfo(countryInput, buildCountryLookup(index))?.a2
    }
  }

  const stateNorms = (stateName ?? '')
    .split(',')
    .map((value) => normalizeName(value))
    .filter(Boolean)

  // Try with the state first, then without (the state may be an abbreviation the data doesn't use).
  let center = findCityCoords(cities, name, countryA2, stateNorms)
  if (!center && stateNorms.length > 0) {
    center = findCityCoords(cities, name, countryA2)
  }
  if (!center) {
    return null
  }

  const matches: { name: string; distance: number }[] = []
  for (const city of cities) {
    if (countryA2 && city.c !== countryA2) continue
    if (typeof city.la !== 'number' || typeof city.lo !== 'number') continue

    const distance = distanceMiles(center.la, center.lo, city.la, city.lo)
    if (distance > miles || distance < 0.5) continue
    matches.push({ name: city.n, distance })
  }

  matches.sort((a, b) => a.distance - b.distance)

  const seen = new Set<string>()
  const names: string[] = []
  let truncated = false
  for (const match of matches) {
    const key = match.name.toLowerCase()
    if (seen.has(key)) continue
    seen.add(key)
    if (names.length >= MAX_CITIES) {
      truncated = true
      break
    }
    names.push(match.name)
  }

  if (names.length === 0) {
    return { names: [], note: `No other towns found within ${miles} miles of ${name}.` }
  }

  return {
    names,
    note: truncated
      ? `Including the nearest ${names.length} towns within ${miles} miles of ${name}.`
      : `Including ${names.length} town${names.length === 1 ? '' : 's'} within ${miles} miles of ${name}.`,
  }
}