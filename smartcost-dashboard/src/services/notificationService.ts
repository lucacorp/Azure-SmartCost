// Service for managing PWA Push Notifications

const PUBLIC_VAPID_KEY = process.env.REACT_APP_VAPID_PUBLIC_KEY || 'YOUR_PUBLIC_VAPID_KEY';
const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:7071/api';

export interface PushSubscriptionData {
  endpoint: string;
  keys: {
    p256dh: string;
    auth: string;
  };
}

class NotificationService {
  private registration: ServiceWorkerRegistration | null = null;

  /**
   * Initialize notification service with service worker registration
   */
  async initialize(registration: ServiceWorkerRegistration) {
    this.registration = registration;
    console.log('[Notifications] Service initialized');
  }

  /**
   * Check if notifications are supported
   */
  isSupported(): boolean {
    return 'Notification' in window && 'serviceWorker' in navigator && 'PushManager' in window;
  }

  /**
   * Get current notification permission status
   */
  getPermissionStatus(): NotificationPermission {
    if (!this.isSupported()) {
      return 'denied';
    }
    return Notification.permission;
  }

  /**
   * Request notification permission from user
   */
  async requestPermission(): Promise<NotificationPermission> {
    if (!this.isSupported()) {
      console.warn('[Notifications] Not supported in this browser');
      return 'denied';
    }

    try {
      const permission = await Notification.requestPermission();
      console.log('[Notifications] Permission:', permission);
      return permission;
    } catch (error) {
      console.error('[Notifications] Permission request failed:', error);
      return 'denied';
    }
  }

  /**
   * Subscribe to push notifications
   */
  async subscribe(tenantId: string): Promise<PushSubscriptionData | null> {
    if (!this.registration) {
      console.error('[Notifications] Service worker not registered');
      return null;
    }

    try {
      // Check permission
      const permission = await this.requestPermission();
      if (permission !== 'granted') {
        console.warn('[Notifications] Permission not granted');
        return null;
      }

      // Subscribe to push
      const subscription = await this.registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: this.urlBase64ToUint8Array(PUBLIC_VAPID_KEY) as BufferSource,
      });

      console.log('[Notifications] Subscribed:', subscription);

      // Convert subscription to plain object
      const subscriptionData = {
        endpoint: subscription.endpoint,
        keys: {
          p256dh: arrayBufferToBase64(subscription.getKey('p256dh')),
          auth: arrayBufferToBase64(subscription.getKey('auth')),
        },
      };

      // Send subscription to backend
      await this.sendSubscriptionToBackend(tenantId, subscriptionData);

      return subscriptionData;
    } catch (error) {
      console.error('[Notifications] Subscribe failed:', error);
      return null;
    }
  }

  /**
   * Unsubscribe from push notifications
   */
  async unsubscribe(tenantId: string): Promise<boolean> {
    if (!this.registration) {
      return false;
    }

    try {
      const subscription = await this.registration.pushManager.getSubscription();
      if (subscription) {
        await subscription.unsubscribe();
        
        // Remove subscription from backend
        await this.removeSubscriptionFromBackend(tenantId, subscription.endpoint);
        
        console.log('[Notifications] Unsubscribed');
        return true;
      }
      return false;
    } catch (error) {
      console.error('[Notifications] Unsubscribe failed:', error);
      return false;
    }
  }

  /**
   * Get current subscription
   */
  async getSubscription(): Promise<PushSubscription | null> {
    if (!this.registration) {
      return null;
    }

    try {
      return await this.registration.pushManager.getSubscription();
    } catch (error) {
      console.error('[Notifications] Get subscription failed:', error);
      return null;
    }
  }

  /**
   * Check if user is subscribed
   */
  async isSubscribed(): Promise<boolean> {
    const subscription = await this.getSubscription();
    return subscription !== null;
  }

  /**
   * Show a local notification (doesn't require push)
   */
  async showLocalNotification(title: string, options?: NotificationOptions) {
    if (!this.registration) {
      return;
    }

    try {
      await this.registration.showNotification(title, {
        icon: '/logo192.png',
        badge: '/logo192.png',
        ...options,
      });
    } catch (error) {
      console.error('[Notifications] Show local notification failed:', error);
    }
  }

  /**
   * Send subscription to backend
   */
  private async sendSubscriptionToBackend(
    tenantId: string,
    subscription: PushSubscriptionData
  ): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/notifications/subscribe`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          tenantId,
          subscription,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to send subscription to backend');
      }

      console.log('[Notifications] Subscription sent to backend');
    } catch (error) {
      console.error('[Notifications] Send subscription failed:', error);
      throw error;
    }
  }

  /**
   * Remove subscription from backend
   */
  private async removeSubscriptionFromBackend(
    tenantId: string,
    endpoint: string
  ): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/notifications/unsubscribe`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          tenantId,
          endpoint,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to remove subscription from backend');
      }

      console.log('[Notifications] Subscription removed from backend');
    } catch (error) {
      console.error('[Notifications] Remove subscription failed:', error);
      throw error;
    }
  }

  /**
   * Convert VAPID key to Uint8Array
   */
  private urlBase64ToUint8Array(base64String: string): Uint8Array {
    const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
      outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
  }
}

/**
 * Helper function to convert ArrayBuffer to Base64
 */
function arrayBufferToBase64(buffer: ArrayBuffer | null): string {
  if (!buffer) return '';
  
  const bytes = new Uint8Array(buffer);
  let binary = '';
  for (let i = 0; i < bytes.byteLength; i++) {
    binary += String.fromCharCode(bytes[i]);
  }
  return window.btoa(binary);
}

// Export singleton instance
export const notificationService = new NotificationService();
export default notificationService;
