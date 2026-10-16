import { feature as topojsonFeature } from 'topojson-client'
import type { Feature, FeatureCollection, GeoJsonProperties, Geometry } from 'geojson'
import type { GeometryCollection, Topology } from 'topojson-specification'

export interface CountryInfo {
  n: string
  o: string
  a2: string
  a3: string
  i?: string
  alt: string[]
}

export interface CityEntry {
  n: string
  a?: string[]
  la: number
  lo: number
  c: string
  s: string
  sn: string
  p: number
}

interface CitiesFile {
  cities: CityEntry[]
}

type RegionProps = {
  name: string
  nameEn?: string
  iso?: string
  postal?: string
}

export type Admin1Feature = Feature<Geometry, RegionProps>
export type WorldFeature = Feature<Geometry, { name: string }>

const geoUrl = (path: string) => `${import.meta.env.BASE_URL}geo/${path}`

const compactFormatter = new Intl.NumberFormat('en', { notation: 'compact', maximumFractionDigits: 1 })

export function formatCompact(value: number): string {
  return compactFormatter.format(value)
}

async function fetchJson<T>(url: string): Promise<T | null> {
  try {
    const response = await fetch(url)
    if (!response.ok) {
      return null
    }
    return (await response.json()) as T
  } catch {
    return null
  }
}

function topoToFeatures<P extends GeoJsonProperties>(json: unknown, objectName: string): FeatureCollection<Geometry, P> {
  const topology = json as Topology & { objects: Record<string, GeometryCollection<P>> }
  return topojsonFeature(topology, topology.objects[objectName]) as unknown as FeatureCollection<Geometry, P>
}

export function normalizeName(value: string): string {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '')
    .replace(/^the/, '')
}

let countriesIndexPromise: Promise<CountryInfo[] | null> | null = null

export function loadCountriesIndex(): Promise<CountryInfo[] | null> {
  countriesIndexPromise ??= fetchJson<CountryInfo[]>(geoUrl('countries.json'))
  return countriesIndexPromise
}

export function buildCountryLookup(index: CountryInfo[]): Map<string, CountryInfo> {
  const lookup = new Map<string, CountryInfo>()
  for (const country of index) {
    const keys = [country.n, country.o, ...country.alt].map(normalizeName).filter(Boolean)
    for (const key of new Set(keys)) {
      if (!lookup.has(key)) {
        lookup.set(key, country)
      }
    }
  }
  return lookup
}

export function findCountryInfo(
  name: string,
  lookup: Map<string, CountryInfo>,
): CountryInfo | undefined {
  return lookup.get(normalizeName(name))
}

let worldFeaturesPromise: Promise<WorldFeature[] | null> | null = null

export function loadWorldFeatures(): Promise<WorldFeature[] | null> {
  worldFeaturesPromise ??= (async () => {
    const json = await fetchJson(geoUrl('countries-110m.json'))
    return json ? topoToFeatures<{ name: string }>(json, 'countries').features : null
  })()
  return worldFeaturesPromise
}

const admin1Cache = new Map<string, Promise<Admin1Feature[] | null>>()

export function loadAdmin1(a3: string): Promise<Admin1Feature[] | null> {
  let cached = admin1Cache.get(a3)
  if (!cached) {
    cached = (async () => {
      const json = await fetchJson(geoUrl(`admin1/${a3}.json`))
      return json ? (topoToFeatures<RegionProps>(json, 'regions').features as Admin1Feature[]) : null
    })()
    admin1Cache.set(a3, cached)
  }
  return cached
}

let citiesPromise: Promise<CityEntry[] | null> | null = null

export function loadCities(): Promise<CityEntry[] | null> {
  citiesPromise ??= fetchJson<CitiesFile>(geoUrl('cities.json')).then((file) => file?.cities ?? null)
  return citiesPromise
}

function isoSuffix(iso: string | undefined): string {
  if (!iso) {
    return ''
  }
  const parts = iso.split('-')
  return normalizeName(parts[parts.length - 1] ?? '')
}

export function matchAdmin1Feature(
  features: Admin1Feature[],
  stateName: string,
): Admin1Feature | undefined {
  const target = normalizeName(stateName)
  if (!target) {
    return undefined
  }
  return (
    features.find((f) => normalizeName(f.properties.name) === target) ??
    features.find((f) => f.properties.nameEn && normalizeName(f.properties.nameEn) === target) ??
    features.find((f) => isoSuffix(f.properties.iso) === target && target.length > 1) ??
    features.find((f) => normalizeName(f.properties.postal ?? '') === target && target.length > 1)
  )
}

export function admin1DisplayName(feature: Admin1Feature): string {
  return feature.properties.nameEn ?? feature.properties.name
}

export function findCityCoords(
  cities: CityEntry[],
  name: string,
  countryA2?: string,
  stateNorms: string[] = [],
): { la: number; lo: number } | null {
  const target = normalizeName(name)
  if (!target) {
    return null
  }
  const candidates = countryA2 ? cities.filter((c) => c.c === countryA2) : cities

  const exact = candidates.filter(
    (c) => normalizeName(c.n) === target || (c.a ?? []).some((alt) => normalizeName(alt) === target),
  )
  let pool = exact
  if (exact.length === 0) {
    pool = candidates.filter((c) => normalizeName(c.n).startsWith(target) && target.length >= 3)
  }
  if (pool.length === 0) {
    return null
  }

  const inState = pool.filter(
    (c) => stateNorms.length === 0 || !c.sn || stateNorms.includes(normalizeName(c.sn)),
  )
  const best = (inState.length > 0 ? inState : pool).reduce((a, b) => (b.p > a.p ? b : a))
  return { la: best.la, lo: best.lo }
}

export function topCityNamesForStates(
  cities: CityEntry[],
  countryA2: string,
  stateNorms: string[],
  perState: number,
  maxTotal: number,
): string[] {
  const stateSet = new Set(stateNorms)
  const byState = new Map<string, CityEntry[]>()
  for (const city of cities) {
    if (city.c !== countryA2 || !city.sn) {
      continue
    }
    const state = normalizeName(city.sn)
    if (!stateSet.has(state)) {
      continue
    }
    const list = byState.get(state)
    if (list) {
      list.push(city)
    } else {
      byState.set(state, [city])
    }
  }

  const picked: CityEntry[] = []
  for (const list of byState.values()) {
    list.sort((a, b) => b.p - a.p)
    picked.push(...list.slice(0, perState))
  }
  picked.sort((a, b) => b.p - a.p)
  return picked.slice(0, maxTotal).map((c) => c.n)
}
