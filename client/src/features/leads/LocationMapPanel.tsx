import { useEffect, useMemo, useRef, useState } from 'react'
import { ChevronRight, Loader2 } from 'lucide-react'
import { useLocationBreakdownMutation } from '@/services/apiSlice'
import type {
  EnrichedProfileSearchResponse,
  LocationBreakdownLevel,
  LocationBreakdownResponse,
  LocationCount,
  ProfileSearchRequest,
} from '@/types/models'
import { ChoroplethMap } from './ChoroplethMap'
import type { MapBubble } from './ChoroplethMap'
import {
  admin1DisplayName,
  buildCountryLookup,
  findCityCoords,
  findCountryInfo,
  formatCompact,
  loadAdmin1,
  loadCities,
  loadCountriesIndex,
  loadWorldFeatures,
  matchAdmin1Feature,
  normalizeName,
  topCityNamesForStates,
} from './geo/geoData'
import type { Admin1Feature, CityEntry, CountryInfo, WorldFeature } from './geo/geoData'

interface LocationMapPanelProps {
  searchRequest: ProfileSearchRequest
  results: EnrichedProfileSearchResponse
  onDrillCountry: (country: string) => void
  onDrillState: (state: string) => void
  onDrillCity: (city: string) => void
}

export function LocationMapPanel({
  searchRequest,
  results,
  onDrillCountry,
  onDrillState,
  onDrillCity,
}: LocationMapPanelProps) {
  const [countryLookup, setCountryLookup] = useState<Map<string, CountryInfo> | null | undefined>(undefined)
  const [worldFeatures, setWorldFeatures] = useState<WorldFeature[] | null | undefined>(undefined)
  const [admin1State, setAdmin1State] = useState<{
    a3: string
    features: Admin1Feature[] | null
  } | null>(null)
  const [cities, setCities] = useState<CityEntry[] | null | undefined>(undefined)
  const [appliedBreakdown, setAppliedBreakdown] = useState<{
    sig: string
    data: LocationBreakdownResponse | null
  } | null>(null)
  const lastFiredRef = useRef<string | null>(null)
  const [breakdownTrigger] = useLocationBreakdownMutation()

  const country = searchRequest.countries?.[0]?.trim() ?? ''
  const stateNames = useMemo(() => searchRequest.states ?? [], [searchRequest])
  const cityNames = useMemo(() => searchRequest.cities ?? [], [searchRequest])

  const breakdownLevel: LocationBreakdownLevel =
    stateNames.length > 0 || cityNames.length > 0 ? 'city' : country ? 'state' : 'country'

  useEffect(() => {
    let ignore = false
    loadCountriesIndex().then((index) => {
      if (!ignore) {
        setCountryLookup(index ? buildCountryLookup(index) : null)
      }
    })
    return () => {
      ignore = true
    }
  }, [])

  useEffect(() => {
    if (breakdownLevel !== 'country' || worldFeatures !== undefined) {
      return
    }
    let ignore = false
    loadWorldFeatures().then((features) => {
      if (!ignore) {
        setWorldFeatures(features)
      }
    })
    return () => {
      ignore = true
    }
  }, [breakdownLevel, worldFeatures])

  useEffect(() => {
    if (breakdownLevel !== 'city' || cities !== undefined) {
      return
    }
    let ignore = false
    loadCities().then((list) => {
      if (!ignore) {
        setCities(list)
      }
    })
    return () => {
      ignore = true
    }
  }, [breakdownLevel, cities])

  const countryInfo = useMemo(
    () => (country && countryLookup ? findCountryInfo(country, countryLookup) : undefined),
    [country, countryLookup],
  )

  const admin1Features =
    countryInfo && admin1State && admin1State.a3 === countryInfo.a3 ? admin1State.features : undefined

  useEffect(() => {
    if (breakdownLevel === 'country' || !countryInfo || admin1State?.a3 === countryInfo.a3) {
      return
    }
    let ignore = false
    const a3 = countryInfo.a3
    loadAdmin1(a3).then((features) => {
      if (!ignore) {
        setAdmin1State({ a3, features: features ?? null })
      }
    })
    return () => {
      ignore = true
    }
  }, [breakdownLevel, countryInfo, admin1State])

  const sampleCounts = useMemo(() => {
    const map = new Map<string, number>()
    for (const result of results.results) {
      const address = result.profile_data?.profile_address
      const raw =
        breakdownLevel === 'country'
          ? address?.country
          : breakdownLevel === 'state'
            ? address?.state
            : address?.city
      const name = raw?.trim()
      if (!name) {
        continue
      }
      map.set(name, (map.get(name) ?? 0) + 1)
    }
    return Array.from(map, ([name, count]): LocationCount => ({ name, count })).sort(
      (a, b) => b.count - a.count,
    )
  }, [results, breakdownLevel])

  const totalMatched = results.counts?.profiles_total_results ?? results.results.length
  const returned = results.results.length
  const needsRefinement = totalMatched > returned

  const admin1Pending =
    breakdownLevel !== 'country' && countryLookup !== undefined && !!countryInfo && admin1Features === undefined
  const canonicalPending =
    breakdownLevel === 'country'
      ? false
      : breakdownLevel === 'state'
        ? countryLookup === undefined || admin1Pending
        : cities === undefined || countryLookup === undefined

  const canonicalRegions = useMemo(() => {
    if (breakdownLevel === 'state' && admin1Features) {
      return admin1Features.map((f) => admin1DisplayName(f))
    }
    if (breakdownLevel === 'city' && cities && countryInfo) {
      return topCityNamesForStates(
        cities,
        countryInfo.a2,
        stateNames.map(normalizeName),
        25,
        60,
      )
    }
    return undefined
  }, [breakdownLevel, admin1Features, cities, countryInfo, stateNames])

  const fireSignature = useMemo(() => {
    if (!needsRefinement || canonicalPending) {
      return null
    }
    return JSON.stringify({
      level: breakdownLevel,
      criteria: searchRequest,
      regions: canonicalRegions,
      total: totalMatched,
    })
  }, [needsRefinement, canonicalPending, breakdownLevel, searchRequest, canonicalRegions, totalMatched])

  useEffect(() => {
    const sig = fireSignature
    if (!sig || lastFiredRef.current === sig) {
      return
    }
    lastFiredRef.current = sig
    void breakdownTrigger({
      search: searchRequest,
      level: breakdownLevel,
      regions: canonicalRegions,
      sample: sampleCounts,
      totalMatched,
      returned,
    })
      .unwrap()
      .then((data) => setAppliedBreakdown({ sig, data }))
      .catch(() => setAppliedBreakdown({ sig, data: null }))
  }, [
    fireSignature,
    breakdownTrigger,
    searchRequest,
    breakdownLevel,
    canonicalRegions,
    sampleCounts,
    totalMatched,
    returned,
  ])

  const appliedEntry = fireSignature && appliedBreakdown?.sig === fireSignature ? appliedBreakdown : null
  const applied = appliedEntry ? appliedEntry.data : undefined
  const breakdownPending = !!fireSignature && !appliedEntry
  const breakdownFailed = !!appliedEntry && appliedEntry.data === null

  const displayCounts = useMemo(() => {
    if (!needsRefinement) {
      return sampleCounts
    }
    return applied?.regions ?? sampleCounts
  }, [needsRefinement, applied, sampleCounts])

  const unlocatedCount = useMemo(() => {
    const sampleTotal = sampleCounts.reduce((sum, entry) => sum + entry.count, 0)
    return Math.max(0, returned - sampleTotal)
  }, [sampleCounts, returned])

  const activeFeatures: (Admin1Feature | WorldFeature)[] | undefined =
    breakdownLevel === 'country' ? (worldFeatures ?? undefined) : (admin1Features ?? undefined)

  const resolved = useMemo(() => {
    const counts = new Map<string, number>()
    const names = new Map<string, string>()
    if (breakdownLevel === 'city' || !activeFeatures) {
      return { counts, names }
    }
    for (const feature of activeFeatures) {
      const featureNorm = normalizeName(feature.properties.name)
      if (!featureNorm) {
        continue
      }
      const admin1 = feature as Admin1Feature
      names.set(featureNorm, admin1.properties.nameEn ?? feature.properties.name)
      for (const entry of displayCounts) {
        let matches = normalizeName(entry.name) === featureNorm
        if (!matches && breakdownLevel === 'country' && countryLookup) {
          const info = findCountryInfo(entry.name, countryLookup)
          matches = !!info && info.i !== undefined && String(feature.id ?? '') === info.i
        }
        if (!matches && breakdownLevel === 'state') {
          const matched = matchAdmin1Feature(activeFeatures as Admin1Feature[], entry.name)
          matches = matched === feature
        }
        if (matches) {
          counts.set(featureNorm, entry.count)
          names.set(featureNorm, entry.name)
          break
        }
      }
    }
    return { counts, names }
  }, [breakdownLevel, activeFeatures, displayCounts, countryLookup])

  const cityMapFeatures = useMemo(() => {
    if (breakdownLevel !== 'city' || !admin1Features) {
      return undefined
    }
    const matched = stateNames
      .map((name) => matchAdmin1Feature(admin1Features, name))
      .filter((feature): feature is Admin1Feature => !!feature)
    const unique = matched.filter((feature, index) => matched.indexOf(feature) === index)
    return unique.length > 0 ? unique : admin1Features
  }, [breakdownLevel, admin1Features, stateNames])

  const bubbles = useMemo(() => {
    if (breakdownLevel !== 'city' || !cities || !countryInfo) {
      return undefined
    }
    const stateNorms = stateNames.map(normalizeName)
    const list: MapBubble[] = []
    for (const entry of displayCounts) {
      if (entry.count <= 0) {
        continue
      }
      const coords = findCityCoords(cities, entry.name, countryInfo.a2, stateNorms)
      if (coords) {
        list.push({ name: entry.name, lat: coords.la, lon: coords.lo, count: entry.count })
      }
    }
    return list
  }, [breakdownLevel, cities, countryInfo, stateNames, displayCounts])

  const selectedCityNorms = useMemo(() => new Set(cityNames.map(normalizeName)), [cityNames])

  const mapFeatures =
    breakdownLevel === 'country'
      ? (worldFeatures ?? undefined)
      : breakdownLevel === 'state'
        ? (admin1Features ?? undefined)
        : cityMapFeatures

  const mapPending =
    breakdownLevel === 'country'
      ? worldFeatures === undefined
      : countryLookup === undefined || (!!countryInfo && admin1Features === undefined)

  const title =
    breakdownLevel === 'country'
      ? 'Candidates by country'
      : breakdownLevel === 'state'
        ? `Candidates by state/region in ${country}`
        : `Candidates by major city in ${stateNames.join(', ') || country}`

  const status = breakdownPending
    ? 'Getting exact counts for each region...'
    : !needsRefinement
      ? `Counts cover all ${totalMatched.toLocaleString()} matching profiles.`
      : breakdownFailed
        ? 'Could not refine exact counts - showing counts from the profiles shown.'
        : applied?.complete
          ? `Exact counts across ${totalMatched.toLocaleString()} matching profiles.`
          : `Counts per region for ${totalMatched.toLocaleString()} matching profiles (some regions unavailable).`

  const drillFromList = (name: string) => {
    if (breakdownLevel === 'country') {
      onDrillCountry(name)
    } else if (breakdownLevel === 'state') {
      onDrillState(name)
    } else {
      onDrillCity(name)
    }
  }

  const onRegionClick = (featureName: string) => {
    const drillName = resolved.names.get(normalizeName(featureName)) ?? featureName
    if (breakdownLevel === 'country') {
      onDrillCountry(drillName)
    } else if (breakdownLevel === 'state') {
      onDrillState(drillName)
    }
  }

  const listLabel =
    breakdownLevel === 'country' ? 'Countries' : breakdownLevel === 'state' ? 'States / regions' : 'Cities'

  return (
    <div className="space-y-3 rounded-md border p-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="space-y-0.5">
          <p className="text-sm font-medium">{title}</p>
          <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
            {breakdownPending && <Loader2 className="h-3 w-3 animate-spin" />}
            {status}
          </p>
        </div>
        <p className="max-w-64 text-right text-xs text-muted-foreground">
          Click a region to focus the search. Counts are Rhetorik approximations.
        </p>
      </div>

      <div className="grid gap-4 lg:grid-cols-[1fr_260px]">
        <div>
          {mapPending ? (
            <div className="flex h-64 items-center justify-center rounded-md border bg-muted/30 text-sm text-muted-foreground">
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Loading map...
            </div>
          ) : mapFeatures && mapFeatures.length > 0 ? (
            <ChoroplethMap
              features={mapFeatures}
              projection={breakdownLevel === 'country' ? 'world' : 'mercator'}
              countsByNorm={breakdownLevel === 'city' ? undefined : resolved.counts}
              onRegionClick={breakdownLevel === 'city' ? undefined : onRegionClick}
              bubbles={bubbles}
              onBubbleClick={onDrillCity}
              highlightBubbleNorms={selectedCityNorms}
            />
          ) : (
            <div className="flex h-64 items-center justify-center rounded-md border bg-muted/30 px-4 text-center text-sm text-muted-foreground">
              Map data unavailable for {country || 'this region'} - use the list to drill down
            </div>
          )}
        </div>

        <div>
          <p className="mb-1 text-xs font-medium text-muted-foreground">{listLabel}</p>
          <ol className="max-h-[420px] divide-y overflow-y-auto rounded-md border text-sm">
            {displayCounts.map((entry) => (
              <li key={entry.name}>
                <button
                  type="button"
                  className="flex w-full items-center justify-between gap-2 px-3 py-1.5 text-left hover:bg-accent"
                  onClick={() => drillFromList(entry.name)}
                >
                  <span className="truncate">{entry.name}</span>
                  <span className="flex shrink-0 items-center gap-1 font-medium">
                    {formatCompact(entry.count)}
                    <ChevronRight className="h-3 w-3 text-muted-foreground" />
                  </span>
                </button>
              </li>
            ))}
            {unlocatedCount > 0 && (
              <li className="flex items-center justify-between gap-2 px-3 py-1.5 text-muted-foreground">
                <span className="truncate">No location data</span>
                <span>{formatCompact(unlocatedCount)}</span>
              </li>
            )}
            {displayCounts.length === 0 && unlocatedCount === 0 && (
              <li className="px-3 py-1.5 text-muted-foreground">No candidates found</li>
            )}
          </ol>
        </div>
      </div>
    </div>
  )
}
