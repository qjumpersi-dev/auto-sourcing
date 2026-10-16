import { useMemo } from 'react'
import { geoMercator, geoNaturalEarth1, geoPath } from 'd3-geo'
import type { Feature, Geometry } from 'geojson'
import { formatCompact, normalizeName } from './geo/geoData'

export interface MapBubble {
  name: string
  lat: number
  lon: number
  count: number
}

interface ChoroplethMapProps {
  features: Feature<Geometry, { name: string }>[]
  projection?: 'world' | 'mercator'
  countsByNorm?: Map<string, number>
  onRegionClick?: (name: string) => void
  highlightNorms?: Set<string>
  bubbles?: MapBubble[]
  onBubbleClick?: (name: string) => void
  highlightBubbleNorms?: Set<string>
  width?: number
  height?: number
}

const LOW = [224, 242, 254]
const HIGH = [7, 89, 133]

function fillColor(count: number, max: number): string {
  if (max <= 0 || count <= 0) {
    return '#f8fafc'
  }
  const t = Math.sqrt(count / max)
  const mix = (i: number) => Math.round(LOW[i] + (HIGH[i] - LOW[i]) * t)
  return `rgb(${mix(0)}, ${mix(1)}, ${mix(2)})`
}

function labelColor(count: number, max: number): string {
  return max > 0 && count > 0 && Math.sqrt(count / max) > 0.55 ? '#ffffff' : '#0f172a'
}

export function ChoroplethMap({
  features,
  projection = 'mercator',
  countsByNorm,
  onRegionClick,
  highlightNorms,
  bubbles,
  onBubbleClick,
  highlightBubbleNorms,
  width = 720,
  height = 380,
}: ChoroplethMapProps) {
  const { path, projectionFn } = useMemo(() => {
    const collection = { type: 'FeatureCollection' as const, features }
    const projectionFn =
      projection === 'world'
        ? geoNaturalEarth1().fitExtent(
            [
              [6, 6],
              [width - 6, height - 6],
            ],
            collection,
          )
        : geoMercator().fitExtent(
            [
              [6, 6],
              [width - 6, height - 6],
            ],
            collection,
          )
    return { path: geoPath(projectionFn), projectionFn }
  }, [features, projection, width, height])

  const maxCount = useMemo(() => {
    let max = 0
    countsByNorm?.forEach((count) => {
      if (count > max) {
        max = count
      }
    })
    return max
  }, [countsByNorm])

  const maxBubble = useMemo(() => {
    let max = 0
    for (const bubble of bubbles ?? []) {
      if (bubble.count > max) {
        max = bubble.count
      }
    }
    return max
  }, [bubbles])

  if (features.length === 0) {
    return null
  }

  return (
    <div className="space-y-1">
      <svg viewBox={`0 0 ${width} ${height}`} className="h-auto w-full select-none" role="img">
        {features.map((feature, index) => {
          const d = path(feature)
          if (!d) {
            return null
          }
          const name = feature.properties.name
          const count = countsByNorm?.get(normalizeName(name)) ?? 0
          return (
            <path
              key={feature.id ?? `${name}-${index}`}
              d={d}
              fill={fillColor(count, maxCount)}
              stroke="#94a3b8"
              strokeWidth={0.5}
              className={onRegionClick ? 'cursor-pointer' : undefined}
              onClick={onRegionClick ? () => onRegionClick(name) : undefined}
            >
              <title>
                {name}
                {count > 0 ? `: ${count.toLocaleString()} candidate${count === 1 ? '' : 's'}` : ''}
              </title>
            </path>
          )
        })}

        {features.map((feature, index) => {
          if (!highlightNorms?.has(normalizeName(feature.properties.name))) {
            return null
          }
          const d = path(feature)
          if (!d) {
            return null
          }
          return (
            <path
              key={`hl-${feature.id ?? `${feature.properties.name}-${index}`}`}
              d={d}
              fill="none"
              stroke="#0f172a"
              strokeWidth={1.5}
              pointerEvents="none"
            />
          )
        })}

        {(bubbles ?? []).map((bubble) => {
          const point = projectionFn([bubble.lon, bubble.lat])
          if (!point) {
            return null
          }
          const radius = 4 + 11 * Math.sqrt(bubble.count / Math.max(maxBubble, 1))
          const highlighted = highlightBubbleNorms?.has(normalizeName(bubble.name))
          return (
            <g
              key={`b-${bubble.name}`}
              className={onBubbleClick ? 'cursor-pointer' : undefined}
              onClick={onBubbleClick ? () => onBubbleClick(bubble.name) : undefined}
            >
              <circle
                cx={point[0]}
                cy={point[1]}
                r={radius}
                fill={highlighted ? '#0f172a' : '#1d4ed8'}
                fillOpacity={0.75}
                stroke={highlighted ? '#f59e0b' : '#ffffff'}
                strokeWidth={highlighted ? 2 : 1}
              >
                <title>
                  {bubble.name}: {bubble.count.toLocaleString()} candidate{bubble.count === 1 ? '' : 's'}
                </title>
              </circle>
              <text
                x={point[0] + radius + 3}
                y={point[1] + 3}
                fontSize={10}
                fontWeight={600}
                fill="#0f172a"
                style={{ paintOrder: 'stroke' }}
                stroke="#ffffff"
                strokeWidth={2.5}
                pointerEvents="none"
              >
                {formatCompact(bubble.count)}
              </text>
            </g>
          )
        })}

        {features.map((feature, index) => {
          const name = feature.properties.name
          const count = countsByNorm?.get(normalizeName(name)) ?? 0
          if (count <= 0) {
            return null
          }
          const centroid = path.centroid(feature)
          if (!Number.isFinite(centroid[0]) || !Number.isFinite(centroid[1])) {
            return null
          }
          return (
            <text
              key={`l-${feature.id ?? `${name}-${index}`}`}
              x={centroid[0]}
              y={centroid[1]}
              fontSize={10}
              fontWeight={600}
              textAnchor="middle"
              dominantBaseline="middle"
              fill={labelColor(count, maxCount)}
              style={{ paintOrder: 'stroke' }}
              stroke={labelColor(count, maxCount) === '#ffffff' ? 'rgba(15,23,42,0.35)' : '#ffffff'}
              strokeWidth={2}
              pointerEvents="none"
            >
              {formatCompact(count)}
            </text>
          )
        })}
      </svg>

      {maxCount > 0 && (
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          <span>0</span>
          <div className="flex">
            {Array.from({ length: 8 }, (_, i) => (
              <span
                key={i}
                className="h-2 w-5"
                style={{ backgroundColor: fillColor(Math.round((maxCount * (i + 1)) / 8), maxCount) }}
              />
            ))}
          </div>
          <span>{formatCompact(maxCount)}+</span>
        </div>
      )}
    </div>
  )
}
