import time
from PyQt5.QtCore import QThread, pyqtSignal
from dispatch import Dispatcher


class ElevatorThread(QThread):
    """
    Thread that periodically updates one elevator via Dispatcher.
    Emits 1-based elevator index on each update.
    """
    update_signal = pyqtSignal(int)

    def __init__(self, dispatcher: Dispatcher, elevator_idx: int):
        super().__init__()
        self.dispatcher = dispatcher
        self.elevator_idx = elevator_idx

    def run(self):
        # Loop until someone calls requestInterruption()
        while not self.isInterruptionRequested():
            # update backend model
            self.dispatcher.update_elevator(self.elevator_idx)
            # notify UI (use 1-based id)
            self.update_signal.emit(self.elevator_idx + 1)
            time.sleep(1)
