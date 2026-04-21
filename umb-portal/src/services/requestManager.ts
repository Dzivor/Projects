const activeControllers = new Set<AbortController>();

export const createTrackedAbortController = (): AbortController => {
  const controller = new AbortController();
  activeControllers.add(controller);
  return controller;
};

export const releaseTrackedAbortController = (
  controller: AbortController,
): void => {
  activeControllers.delete(controller);
};

export const cancelAllTrackedRequests = (): void => {
  activeControllers.forEach((controller) => controller.abort());
  activeControllers.clear();
};
