package net.kozhanov.battleship.app

import android.R.id
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.FragmentManager
import androidx.fragment.app.FragmentTransaction
import net.kozhanov.battleship.R
import net.kozhanov.battleship.R.layout
import net.kozhanov.battleship.features.board.BoardFragment


class MainActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(layout.activity_main)

        val transaction: FragmentTransaction = supportFragmentManager.beginTransaction()
        transaction.add(R.id.fragmentContainer, BoardFragment(), "board")
        transaction.commit()
    }
}