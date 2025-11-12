import React, { useEffect, useState } from 'react';
import './PWAInstallPrompt.css';

interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

const PWAInstallPrompt: React.FC = () => {
  const [deferredPrompt, setDeferredPrompt] = useState<BeforeInstallPromptEvent | null>(null);
  const [showInstallPrompt, setShowInstallPrompt] = useState(false);
  const [isStandalone, setIsStandalone] = useState(false);
  const [isIOS, setIsIOS] = useState(false);
  const [showIOSInstructions, setShowIOSInstructions] = useState(false);

  useEffect(() => {
    // Check if running in standalone mode
    const checkStandalone = () => {
      const isStandaloneMode = window.matchMedia('(display-mode: standalone)').matches
        || (window.navigator as any).standalone
        || document.referrer.includes('android-app://');
      
      setIsStandalone(isStandaloneMode);
    };

    // Check if iOS
    const checkIOS = () => {
      const userAgent = window.navigator.userAgent.toLowerCase();
      const isIOSDevice = /iphone|ipad|ipod/.test(userAgent);
      setIsIOS(isIOSDevice);
    };

    checkStandalone();
    checkIOS();

    // Listen for beforeinstallprompt event (Android/Chrome)
    const handleBeforeInstallPrompt = (e: Event) => {
      console.log('[PWA] beforeinstallprompt event fired');
      e.preventDefault();
      setDeferredPrompt(e as BeforeInstallPromptEvent);
      
      // Show install prompt after 5 seconds
      setTimeout(() => {
        if (!isStandalone) {
          setShowInstallPrompt(true);
        }
      }, 5000);
    };

    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt);

    // Listen for app installed event
    const handleAppInstalled = () => {
      console.log('[PWA] App installed');
      setShowInstallPrompt(false);
      setDeferredPrompt(null);
    };

    window.addEventListener('appinstalled', handleAppInstalled);

    return () => {
      window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
      window.removeEventListener('appinstalled', handleAppInstalled);
    };
  }, [isStandalone]);

  const handleInstallClick = async () => {
    if (!deferredPrompt) {
      // If iOS, show instructions
      if (isIOS) {
        setShowIOSInstructions(true);
        return;
      }
      return;
    }

    setShowInstallPrompt(false);

    // Show native install prompt
    await deferredPrompt.prompt();

    // Wait for user response
    const { outcome } = await deferredPrompt.userChoice;
    console.log(`[PWA] User response: ${outcome}`);

    if (outcome === 'accepted') {
      console.log('[PWA] User accepted the install prompt');
    } else {
      console.log('[PWA] User dismissed the install prompt');
    }

    setDeferredPrompt(null);
  };

  const handleDismiss = () => {
    setShowInstallPrompt(false);
    // Remember dismissal in localStorage
    localStorage.setItem('pwa-install-dismissed', Date.now().toString());
  };

  // Don't show if already installed or dismissed recently
  useEffect(() => {
    const dismissed = localStorage.getItem('pwa-install-dismissed');
    if (dismissed) {
      const dismissedTime = parseInt(dismissed);
      const oneDayAgo = Date.now() - (24 * 60 * 60 * 1000);
      if (dismissedTime > oneDayAgo) {
        setShowInstallPrompt(false);
      }
    }
  }, []);

  // iOS Instructions Modal
  if (showIOSInstructions) {
    return (
      <div className="pwa-ios-modal">
        <div className="pwa-ios-content">
          <button className="pwa-ios-close" onClick={() => setShowIOSInstructions(false)}>
            ×
          </button>
          <h3>📱 Install on iOS</h3>
          <p>To install this app on your iPhone or iPad:</p>
          <ol>
            <li>
              Tap the <strong>Share</strong> button 
              <span className="ios-icon">⎙</span> in Safari
            </li>
            <li>
              Scroll down and tap <strong>"Add to Home Screen"</strong> 
              <span className="ios-icon">➕</span>
            </li>
            <li>
              Tap <strong>"Add"</strong> in the top right corner
            </li>
          </ol>
          <button className="pwa-ios-got-it" onClick={() => setShowIOSInstructions(false)}>
            Got it!
          </button>
        </div>
      </div>
    );
  }

  // Android/Chrome Install Prompt
  if (showInstallPrompt && !isStandalone) {
    return (
      <div className="pwa-install-banner">
        <div className="pwa-install-content">
          <div className="pwa-install-icon">
            <img src="/logo192.png" alt="SmartCost" />
          </div>
          <div className="pwa-install-text">
            <h4>Install Azure SmartCost</h4>
            <p>Get quick access from your home screen</p>
          </div>
          <div className="pwa-install-actions">
            <button className="pwa-install-btn" onClick={handleInstallClick}>
              Install
            </button>
            <button className="pwa-dismiss-btn" onClick={handleDismiss}>
              ✕
            </button>
          </div>
        </div>
      </div>
    );
  }

  return null;
};

export default PWAInstallPrompt;
