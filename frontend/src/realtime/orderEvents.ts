import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack'
import { onBeforeUnmount, ref, watch, type Ref } from 'vue'
import { getAccessToken, realtimeBaseUrl, refreshSession } from '@/api/client'
import type { OrderEventDto } from '@/api/types'

const EVENT_METHOD = 'orderEvent'

/**
 * REST-ответы приходят в camelCase (System.Text.Json), а MessagePack сериализует
 * реальные имена свойств C# — то есть PascalCase. Приводим к одному виду здесь,
 * чтобы дальше по коду была ровно одна форма события.
 */
interface RawOrderEvent {
  OrderId?: string
  Number?: string
  Kind?: string
  Status?: string
  StatusLabel?: string
  Message?: string | null
  At?: string
}

function normalize(raw: RawOrderEvent & Partial<OrderEventDto>): OrderEventDto {
  return {
    orderId: raw.orderId ?? raw.OrderId ?? '',
    number: raw.number ?? raw.Number ?? '',
    kind: (raw.kind ?? raw.Kind ?? 'StatusChanged') as OrderEventDto['kind'],
    status: (raw.status ?? raw.Status ?? 'New') as OrderEventDto['status'],
    statusLabel: raw.statusLabel ?? raw.StatusLabel ?? '',
    message: raw.message ?? raw.Message ?? null,
    at: raw.at ?? raw.At ?? new Date().toISOString(),
  }
}

function createConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${realtimeBaseUrl}/hubs/orders`, {
      // WebSocket не умеет слать заголовок Authorization — токен уезжает query-параметром.
      accessTokenFactory: async () => getAccessToken() ?? (await refreshSession()) ?? '',
    })
    .withHubProtocol(new MessagePackHubProtocol())
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()
}

/**
 * Подписка на события одной заявки. Живые обновления — приятный бонус:
 * если хаб недоступен, страница просто работает как обычная, без ошибок в лицо пользователю.
 */
export function useOrderEvents(orderId: Ref<string>, onEvent: (event: OrderEventDto) => void) {
  const connected = ref(false)
  let connection: HubConnection | null = null
  let subscribedTo: string | null = null

  async function subscribe(id: string): Promise<void> {
    if (!connection || connection.state !== HubConnectionState.Connected) {
      return
    }

    try {
      if (subscribedTo && subscribedTo !== id) {
        await connection.invoke('UnsubscribeFromOrder', subscribedTo)
      }

      await connection.invoke('SubscribeToOrder', id)
      subscribedTo = id
    } catch {
      // Нет доступа или заявка исчезла — живых обновлений просто не будет.
    }
  }

  async function start(): Promise<void> {
    connection = createConnection()
    connection.on(EVENT_METHOD, (raw: RawOrderEvent) => onEvent(normalize(raw)))
    connection.onreconnected(async () => {
      connected.value = true
      subscribedTo = null
      await subscribe(orderId.value)
    })
    connection.onclose(() => (connected.value = false))

    try {
      await connection.start()
      connected.value = true
      await subscribe(orderId.value)
    } catch {
      connected.value = false
    }
  }

  void start()

  watch(orderId, (id) => void subscribe(id))

  onBeforeUnmount(() => {
    connection?.stop().catch(() => undefined)
    connection = null
  })

  return { connected }
}
