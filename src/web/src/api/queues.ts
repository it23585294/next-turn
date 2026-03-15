/**
 * Queue API calls.
 */
import { apiClient, parseApiError } from './client'
import { getToken } from '../utils/authToken'

/** Shape returned by POST /api/queues/{queueId}/join on HTTP 200. */
export interface JoinQueueResult {
  ticketNumber: number
  positionInQueue: number
  estimatedWaitSeconds: number
}

/** Shape returned by GET /api/queues/{queueId}/status on HTTP 200. */
export interface QueueStatusResult {
  ticketNumber: number
  positionInQueue: number
  estimatedWaitSeconds: number
  queueStatus: 'Active' | 'Paused' | 'Closed'
}

/** Shape returned by POST /api/queues on HTTP 201. */
export interface CreateQueueResult {
  queueId: string
  shareableLink: string
}

/** Shape returned by GET /api/queues (org admin list). */
export interface OrgQueueSummary {
  queueId: string
  name: string
  maxCapacity: number
  averageServiceTimeSeconds: number
  status: string
  shareableLink: string
}

export interface CreateQueueBody {
  name: string
  maxCapacity: number
  averageServiceTimeSeconds: number
}

/**
 * POST /api/queues/{queueId}/join
 *
 * The API extracts the userId from the JWT sub claim server-side — the client
 * never passes a userId in the body (prevents impersonation).
 *
 * @throws ApiError on:
 *   400 — queue not found
 *   401 — missing or invalid JWT
 *   409 — already in queue (detail: "Already in this queue.")
 *         OR queue full   (raw.canBookAppointment === true)
 *   422 — validation failed
 */
export async function joinQueue(
  queueId: string,
  tenantId: string,
): Promise<JoinQueueResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<JoinQueueResult>(
      `/queues/${queueId}/join`,
      null,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': tenantId,
        },
      }
    )
    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

/**
 * GET /api/queues/{queueId}/status
 *
 * Returns the authenticated user's current position and ETA.
 * Called by the frontend polling loop every 30 seconds after joining.
 *
 * @throws ApiError on:
 *   400 — queue not found, or user has no active entry
 *   401 — missing or invalid JWT
 */
export async function getQueueStatus(
  queueId: string,
  tenantId: string,
): Promise<QueueStatusResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<QueueStatusResult>(
      `/queues/${queueId}/status`,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': tenantId,
        },
      }
    )
    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

/**
 * POST /api/queues
 *
 * Creates a new queue for the authenticated org admin's organisation.
 * OrganisationId is taken from the JWT tid claim server-side.
 *
 * @throws ApiError on:
 *   400 — organisation not found
 *   401 — missing or invalid JWT
 *   403 — role is not OrgAdmin or SystemAdmin
 *   422 — validation failed (name empty, capacity < 1, avgTime < 1)
 */
export async function createQueue(
  tenantId: string,
  body: CreateQueueBody,
): Promise<CreateQueueResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<CreateQueueResult>(
      `/queues`,
      body,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': tenantId,
        },
      }
    )
    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

/**
 * GET /api/queues
 *
 * Lists all queues for the authenticated org admin's organisation.
 * Used by the admin dashboard on page load.
 *
 * @throws ApiError on:
 *   401 — missing or invalid JWT
 *   403 — role is not OrgAdmin or SystemAdmin
 */
export async function getOrgQueues(
  tenantId: string,
): Promise<OrgQueueSummary[]> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<OrgQueueSummary[]>(
      `/queues`,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': tenantId,
        },
      }
    )
    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

/**
 * GET /api/queues/browse
 *
 * Lists all queues for the authenticated user's organisation.
 * Accessible to any authenticated role (User, Staff, OrgAdmin, SystemAdmin).
 * Used by the user dashboard so regular users can see and join available queues.
 *
 * @throws ApiError on:
 *   401 — missing or invalid JWT
 */
export async function getAvailableQueues(
  tenantId: string,
): Promise<OrgQueueSummary[]> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<OrgQueueSummary[]>(
      `/queues/browse`,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': tenantId,
        },
      }
    )
    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

/** A queue the current user has an active ticket in. */
export interface MyQueueEntry {
  queueId:        string
  organisationId: string
  queueName:      string
  ticketNumber:   number
  queueStatus:    string
}

/**
 * GET /api/queues/my-entries
 * Returns the queues the authenticated user is currently active in
 * (entry status Waiting or Serving).
 * No X-Tenant-Id required — the user's JWT sub identifies them globally.
 */
export async function getMyQueues(): Promise<MyQueueEntry[]> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<MyQueueEntry[]>(`/queues/my-entries`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

/**
 * POST /api/queues/{queueId}/leave
 *
 * Cancels the authenticated user's active entry in a queue.
 * The API extracts the userId from the JWT sub claim server-side.
 *
 * @throws ApiError on:
 *   400 — queue not found, or user is not in this queue
 *   401 — missing or invalid JWT
 *   422 — validation failed (malformed queueId GUID)
 */
export async function leaveQueue(
  queueId: string,
  tenantId: string,
): Promise<void> {
  try {
    const token = getToken()
    await apiClient.post(
      `/queues/${queueId}/leave`,
      null,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': tenantId,
        },
      }
    )
  } catch (err) {
    throw parseApiError(err)
  }
}

export interface QueueDashboardEntry {
  entryId: string
  ticketNumber: number
  joinedAt: string
}

export interface QueueDashboardResult {
  queueId: string
  queueName: string
  queueStatus: 'Active' | 'Paused' | 'Closed'
  waitingCount: number
  currentlyServing: QueueDashboardEntry | null
  waitingEntries: QueueDashboardEntry[]
}

export interface QueueEntryActionResult {
  entryId: string
  ticketNumber: number
  status: 'Serving' | 'Served' | 'NoShow'
}

/**
 * GET /api/queues/{queueId}/dashboard
 */
export async function getQueueDashboard(
  queueId: string,
  tenantId: string,
): Promise<QueueDashboardResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<QueueDashboardResult>(
      `/queues/${queueId}/dashboard`,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': tenantId,
        },
      }
    )
    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

/**
 * POST /api/queues/{queueId}/call-next
 */
export async function callNext(
  queueId: string,
  tenantId: string,
): Promise<QueueEntryActionResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<QueueEntryActionResult>(
      `/queues/${queueId}/call-next`,
      null,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': tenantId,
        },
      }
    )
    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

/**
 * POST /api/queues/{queueId}/served
 */
export async function markServed(
  queueId: string,
  tenantId: string,
): Promise<QueueEntryActionResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<QueueEntryActionResult>(
      `/queues/${queueId}/served`,
      null,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': tenantId,
        },
      }
    )
    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

/**
 * POST /api/queues/{queueId}/no-show
 */
export async function markNoShow(
  queueId: string,
  tenantId: string,
): Promise<QueueEntryActionResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<QueueEntryActionResult>(
      `/queues/${queueId}/no-show`,
      null,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': tenantId,
        },
      }
    )
    return data
  } catch (err) {
    throw parseApiError(err)
  }
}
