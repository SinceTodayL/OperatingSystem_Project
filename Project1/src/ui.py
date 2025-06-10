import sys
from functools import partial
from PyQt5.QtWidgets import (
    QApplication, QWidget, QLabel, QGridLayout, QPushButton,
    QLCDNumber, QTextEdit, QTextBrowser
)
from PyQt5.QtCore import Qt
from PyQt5.QtGui import QFont

from dispatch import Dispatcher, ELEVATOR_NUM, FLOOR_NUM
from base import ElevatorThread


class ElevatorUI(QWidget):
    """
    Main window for elevator dispatch simulation.
    """
    def __init__(self, dispatcher: Dispatcher):
        super().__init__()
        self.dispatcher = dispatcher
        self.info_log = None
        self.threads = []
        self._setup_ui()
        self._start_threads()

    def _setup_ui(self):
        """
        Set up UI layout: labels, displays, buttons, log and exit.
        """
        grid = QGridLayout()

        # 1) Elevator labels
        for i in range(ELEVATOR_NUM):
            lbl = QLabel(f"Elevator {i + 1}")
            lbl.setAlignment(Qt.AlignCenter)
            lbl.setFont(QFont("Times new roman", 14))
            grid.addWidget(lbl, 0, i, 1, 1)

        # 2) LCD display and state label
        for i in range(ELEVATOR_NUM):
            lcd = QLCDNumber()
            lcd.setObjectName(f"elevatorLCD{i + 1}")
            lcd.setDigitCount(2)
            lcd.display(1)
            grid.addWidget(lcd, 1, i, 1, 1)

            state_lbl = QLabel("Stay")
            state_lbl.setObjectName(f"elevatorState{i + 1}")
            state_lbl.setAlignment(Qt.AlignCenter)
            state_lbl.setFont(QFont("Times new roman", 12))
            state_lbl.setStyleSheet("background:black; color:#93D5DC;")
            grid.addWidget(state_lbl, 2, i, 1, 1)

        # 3) Internal floor buttons
        for eid in range(1, ELEVATOR_NUM + 1):
            for floor in range(1, FLOOR_NUM + 1):
                btn = QPushButton(f"{floor}F")
                btn.clicked.connect(partial(self._on_internal, eid, floor))
                grid.addWidget(btn, floor + 2, eid - 1, 1, 1)

        # 4) OPEN / CLOSE / ALERT buttons
        for eid in range(1, ELEVATOR_NUM + 1):
            o_btn = QPushButton("OPEN")
            o_btn.clicked.connect(partial(self._on_open, eid))
            c_btn = QPushButton("CLOSE")
            c_btn.clicked.connect(partial(self._on_close, eid))
            a_btn = QPushButton("ALERT")
            a_btn.clicked.connect(partial(self._on_alert, eid))
            base_row = FLOOR_NUM + 3
            grid.addWidget(o_btn, base_row, eid - 1, 1, 1)
            grid.addWidget(c_btn, base_row + 1, eid - 1, 1, 1)
            grid.addWidget(a_btn, base_row + 2, eid - 1, 1, 1)

        # 5) External up/down buttons
        for floor in range(1, FLOOR_NUM + 1):
            up = QPushButton(f"▲ {floor}F")
            up.clicked.connect(partial(self._on_external, floor, "up"))
            grid.addWidget(up, floor + 4, ELEVATOR_NUM, 1, 1)
            down = QPushButton(f"▼ {floor}F")
            down.clicked.connect(partial(self._on_external, floor, "down"))
            grid.addWidget(down, floor + 4, ELEVATOR_NUM + 1, 1, 1)

        # 6) Information log
        self.info_log = QTextEdit()
        self.info_log.setReadOnly(True)
        grid.addWidget(self.info_log, 1, ELEVATOR_NUM, 4, 2)

        # 7) Notes
        note = QTextBrowser()
        note.setText("Elevator simulation")
        grid.addWidget(note, 0, ELEVATOR_NUM, 1, 2)

        # 8) Exit button
        exit_button = QPushButton("Exit")
        exit_button.setStyleSheet("background-color: red; color: white; font-weight: bold;")
        exit_button.clicked.connect(self.close)
        grid.addWidget(exit_button, FLOOR_NUM + 4, ELEVATOR_NUM, 1, 2)

        self.setLayout(grid)
        self.setWindowTitle("Elevator Dispatch Simulation")
        self.resize(1300, 700)
        self.show()

    def _start_threads(self):
        """
        Start background threads for each elevator.
        """
        for idx in range(ELEVATOR_NUM):
            thread = ElevatorThread(self.dispatcher, idx)
            thread.update_signal.connect(self._update_ui)
            thread.start()
            self.threads.append(thread)

    def closeEvent(self, event):
        """
        Called when window closes: request threads to stop, then wait for them.
        """
        for thread in self.threads:
            thread.requestInterruption()
            thread.wait()
        event.accept()

    def _update_ui(self, elevator_id: int):
        """
        Update the UI elements for one elevator.
        """
        idx = elevator_id - 1
        lcd = self.findChild(QLCDNumber, f"elevatorLCD{elevator_id}")
        lcd.display(self.dispatcher.floors[idx])

        lbl = self.findChild(QLabel, f"elevatorState{elevator_id}")
        if self.dispatcher.alerts[idx]:
            lbl.setText("Stall")
            lbl.setStyleSheet("background:red; color:yellow;")
        elif self.dispatcher.opens[idx]:
            lbl.setText("Open")
            lbl.setStyleSheet("background:white; color:#5CB3CC;")
        else:
            st = self.dispatcher.states[idx]
            lbl.setText("↑" if st == 1 else "↓" if st == -1 else "Stay")
            lbl.setStyleSheet("background:black; color:#93D5DC;")

    def _on_internal(self, elevator_id: int, floor: int):
        """
        Handle internal request button.
        """
        self.dispatcher.assign_internal(elevator_id, floor)
        self.info_log.append(f"Elevator {elevator_id} scheduled to go to floor {floor}")

    def _on_external(self, floor: int, direction: str):
        """
        Handle external up/down request button.
        """
        eid = self.dispatcher.assign_external(floor, direction)
        if eid > 0:
            self.info_log.append(f"External {direction} call at {floor}F → Elevator {eid}")
        else:
            self.info_log.append(f"No available elevator for external call at {floor}F")

    def _on_open(self, elevator_id: int):
        """
        Handle open button: only works if elevator is idle and not in alert.
        """
        idx = elevator_id - 1
        if self.dispatcher.alerts[idx]:
            self.info_log.append(f"Elevator {elevator_id} is in ALERT state; cannot open doors.")
            return
        if self.dispatcher.states[idx] != 0:
            self.info_log.append(f"Elevator {elevator_id} is moving; cannot open doors.")
            return
        self.dispatcher.opens[idx] = True
        self.info_log.append(f"Elevator {elevator_id} door open requested")
        self._update_ui(elevator_id)

    def _on_close(self, elevator_id: int):
        """
        Handle close button: only works if elevator is idle and not in alert.
        """
        idx = elevator_id - 1
        if self.dispatcher.states[idx] == 0 and not self.dispatcher.alerts[idx]:
            self.dispatcher.opens[idx] = False
            self.info_log.append(f"Elevator {elevator_id} door close requested")
            self._update_ui(elevator_id)

    def _on_alert(self, elevator_id: int):
        """
        Toggle alert mode for the elevator.
        """
        new_status = self.dispatcher.toggle_alert(elevator_id)
        if new_status:
            self.info_log.append(f"Elevator {elevator_id} entered ALERT; reassigning calls")
        else:
            self.info_log.append(f"Elevator {elevator_id} alert cleared")
